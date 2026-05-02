#include "framework.h"
#include <windows.h>
#include <winhttp.h>
#include <wininet.h>
#include <shellapi.h>
#include <commctrl.h>
#include <string>
#include <thread>
#include <fstream>

#pragma comment(lib, "winhttp.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "wininet.lib")
#pragma comment(lib, "comctl32.lib")

// ---------------- RESOURCE IDS ----------------
#include "resource.h"

// ---------------- STATE ----------------
HINSTANCE hInst;
HWND hWndMain;
HWND hProgress;

std::wstring statusText = L"Idle";
bool windowsInstalled = false;

double downloadCurrentMB = 0;
double downloadTotalMB = 0;

std::wstring QUEST_PACKAGE = L"com.MahdiStudios.Martani360";

// ---------------- APPS ----------------
struct AppStep {
    std::wstring name;
    std::wstring url;
    std::wstring output;
};

AppStep questApp = {
    L"Quest",
    L"https://github.com/mahdiisdumb/martani-360/releases/latest/download/Quest.apk",
    L"Quest.apk"
};

AppStep windowsApp = {
    L"Windows",
    L"https://github.com/mahdiisdumb/martani-360/releases/latest/download/Windows.zip",
    L"Windows.zip"
};

// ---------------- UI ----------------
void SetStatus(const std::wstring& s)
{
    statusText = s;
    InvalidateRect(hWndMain, NULL, TRUE);
}

void SetProgress(int p)
{
    SendMessage(hProgress, PBM_SETPOS, p, 0);
}

// ---------------- FILE ----------------
bool FileExists(const wchar_t* path)
{
    return GetFileAttributesW(path) != INVALID_FILE_ATTRIBUTES;
}

void LoadWindowsState()
{
    windowsInstalled = FileExists(L".\\Windows\\installed.flag");
}

void SaveWindowsState()
{
    std::ofstream f(".\\Windows\\installed.flag");
    f << "1";
    windowsInstalled = true;
}

// ---------------- EMBEDDED ADB EXTRACTION ----------------
bool ExtractResource(int id, const wchar_t* outPath)
{
    HRSRC res = FindResourceW(NULL, MAKEINTRESOURCEW(id), L"BINARY");
    if (!res) return false;

    HGLOBAL loaded = LoadResource(NULL, res);
    if (!loaded) return false;

    DWORD size = SizeofResource(NULL, res);
    void* data = LockResource(loaded);

    std::ofstream file(outPath, std::ios::binary);
    file.write((char*)data, size);
    return true;
}

bool EnsureADB()
{
    system("mkdir tools\\adb");

    if (!FileExists(L".\\tools\\adb\\adb.exe"))
    {
        SetStatus(L"Extracting ADB...");

        ExtractResource(IDR_ADB_EXE, L".\\tools\\adb\\adb.exe");
        ExtractResource(IDR_ADB_DLL1, L".\\tools\\adb\\AdbWinApi.dll");
        ExtractResource(IDR_ADB_DLL2, L".\\tools\\adb\\AdbWinUsbApi.dll");
    }

    return FileExists(L".\\tools\\adb\\adb.exe");
}

// ---------------- ADB ----------------
bool RunADB(const std::wstring& args)
{
    std::wstring cmd = L".\\tools\\adb\\adb.exe " + args;

    STARTUPINFOW si{};
    PROCESS_INFORMATION pi{};
    si.cb = sizeof(si);

    if (!CreateProcessW(NULL, cmd.data(), NULL, NULL, FALSE,
        CREATE_NO_WINDOW, NULL, NULL, &si, &pi))
        return false;

    WaitForSingleObject(pi.hProcess, INFINITE);

    DWORD code = 1;
    GetExitCodeProcess(pi.hProcess, &code);

    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);

    return code == 0;
}

void ForceReplacePackage(const std::wstring& package)
{
    RunADB(L"uninstall " + package);
}

// ---------------- DOWNLOAD ----------------
bool DownloadFile(const std::wstring& url, const std::wstring& out)
{
    HINTERNET hInternet = InternetOpenW(L"Downloader", INTERNET_OPEN_TYPE_DIRECT, NULL, NULL, 0);
    if (!hInternet) return false;

    HINTERNET hFile = InternetOpenUrlW(hInternet, url.c_str(), NULL, 0,
        INTERNET_FLAG_RELOAD | INTERNET_FLAG_NO_CACHE_WRITE, 0);

    if (!hFile)
    {
        InternetCloseHandle(hInternet);
        return false;
    }

    std::ofstream file(out, std::ios::binary);

    DWORD contentLength = 0;
    DWORD lenSize = sizeof(contentLength);

    HttpQueryInfoW(hFile,
        HTTP_QUERY_CONTENT_LENGTH | HTTP_QUERY_FLAG_NUMBER,
        &contentLength, &lenSize, NULL);

    downloadTotalMB = contentLength / 1024.0 / 1024.0;

    char buffer[8192];
    DWORD bytesRead = 0;
    DWORD totalRead = 0;

    while (InternetReadFile(hFile, buffer, sizeof(buffer), &bytesRead) && bytesRead)
    {
        file.write(buffer, bytesRead);
        totalRead += bytesRead;

        downloadCurrentMB = totalRead / 1024.0 / 1024.0;

        int percent = contentLength > 0
            ? (int)((double)totalRead / contentLength * 100.0)
            : 0;

        SetProgress(percent);
        InvalidateRect(hWndMain, NULL, TRUE);
    }

    file.close();
    InternetCloseHandle(hFile);
    InternetCloseHandle(hInternet);

    return true;
}

// ---------------- INSTALL QUEST ----------------
void InstallQuest()
{
    SetProgress(0);
    SetStatus(L"Downloading Quest...");

    DownloadFile(questApp.url, questApp.output);

    SetStatus(L"Waiting ADB...");
    EnsureADB();
    RunADB(L"wait-for-device");
    SetProgress(40);

    SetStatus(L"Removing old version...");
    ForceReplacePackage(QUEST_PACKAGE);
    SetProgress(60);

    SetStatus(L"Installing APK...");
    RunADB(L"install -r -d -g Quest.apk");
    SetProgress(100);

    SetStatus(L"Quest installed");
}

// ---------------- INSTALL WINDOWS ----------------
void InstallWindows()
{
    SetProgress(0);
    SetStatus(L"Downloading Windows...");

    DownloadFile(windowsApp.url, windowsApp.output);

    SetProgress(60);
    SetStatus(L"Extracting...");

    system("powershell Expand-Archive -Force Windows.zip .\\Windows");

    SaveWindowsState();

    SetProgress(100);
    SetStatus(L"Windows installed");
}

// ---------------- CLICK ----------------
void HandleClick(int x, int y)
{
    POINT p = { x, y };

    RECT quest = { 60, 80, 460, 170 };
    RECT windows = { 60, 190, 460, 280 };
    RECT launch = { 60, 300, 460, 360 };

    if (PtInRect(&quest, p))
        std::thread(InstallQuest).detach();

    if (PtInRect(&windows, p))
        std::thread(InstallWindows).detach();

    if (windowsInstalled && PtInRect(&launch, p))
    {
        ShellExecuteW(NULL, L"open",
            L".\\Windows\\Martani 360.exe",
            NULL, NULL, SW_SHOW);
    }
}

// ---------------- DRAW ----------------
LRESULT CALLBACK WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    switch (msg)
    {
    case WM_LBUTTONDOWN:
        HandleClick(LOWORD(lParam), HIWORD(lParam));
        break;

    case WM_PAINT:
    {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hWnd, &ps);

        RECT rc;
        GetClientRect(hWnd, &rc);

        HBRUSH bg = CreateSolidBrush(RGB(10, 10, 10));
        FillRect(hdc, &rc, bg);
        DeleteObject(bg);

        SetBkMode(hdc, TRANSPARENT);
        SetTextColor(hdc, RGB(220, 220, 220));

        TextOutW(hdc, 20, 20, L"Martani Launcher", 17);

        EndPaint(hWnd, &ps);
    }
    break;

    case WM_DESTROY:
        PostQuitMessage(0);
        break;

    default:
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }
    return 0;
}

// ---------------- ENTRY ----------------
int APIENTRY wWinMain(HINSTANCE hInstance,
    HINSTANCE, LPWSTR, int nCmdShow)
{
    hInst = hInstance;

    LoadWindowsState();

    INITCOMMONCONTROLSEX icc{};
    icc.dwSize = sizeof(icc);
    icc.dwICC = ICC_PROGRESS_CLASS;
    InitCommonControlsEx(&icc);

    WNDCLASS wc{};
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = L"MartaniLauncher";
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);

    RegisterClass(&wc);

    hWndMain = CreateWindowW(wc.lpszClassName,
        L"Martani Launcher",
        WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT, CW_USEDEFAULT,
        520, 520,
        NULL, NULL,
        hInstance, NULL);

    hProgress = CreateWindowExW(0, PROGRESS_CLASSW, NULL,
        WS_CHILD | WS_VISIBLE,
        60, 420, 400, 20,
        hWndMain, NULL, hInstance, NULL);

    SendMessage(hProgress, PBM_SETRANGE, 0, MAKELPARAM(0, 100));

    EnsureADB();

    ShowWindow(hWndMain, nCmdShow);
    UpdateWindow(hWndMain);

    MSG msg;
    while (GetMessage(&msg, NULL, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    return 0;
}