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

// ---------------- UI HELPERS ----------------
RECT rQuest = { 60, 80, 460, 170 };
RECT rWindows = { 60, 190, 460, 280 };
RECT rLaunch = { 60, 300, 460, 360 };

void SetStatus(const std::wstring& s)
{
    statusText = s;
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

// ---------------- DRAW ----------------
void DrawButton(HDC hdc, RECT rc, const std::wstring& text, bool enabled = true)
{
    HBRUSH bg = CreateSolidBrush(enabled ? RGB(40, 40, 40) : RGB(20, 20, 20));
    FillRect(hdc, &rc, bg);
    DeleteObject(bg);

    SetBkMode(hdc, TRANSPARENT);
    SetTextColor(hdc, RGB(255, 255, 255));

    DrawTextW(hdc, text.c_str(), -1, &rc,
        DT_CENTER | DT_VCENTER | DT_SINGLELINE);
}

// ---------------- ADB / DOWNLOAD (UNCHANGED CORE) ----------------
// (kept minimal, not rewriting your whole backend)

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

    char buffer[8192];
    DWORD bytesRead = 0;
    DWORD totalRead = 0;

    while (InternetReadFile(hFile, buffer, sizeof(buffer), &bytesRead) && bytesRead)
    {
        file.write(buffer, bytesRead);
        totalRead += bytesRead;

        int percent = contentLength > 0
            ? (int)((double)totalRead / contentLength * 100.0)
            : 0;

        SetProgress(percent);
    }

    file.close();
    InternetCloseHandle(hFile);
    InternetCloseHandle(hInternet);

    return true;
}

// ---------------- CLICK ----------------
void HandleClick(int x, int y)
{
    POINT p = { x, y };

    if (PtInRect(&rQuest, p))
        std::thread([] {
        SetStatus(L"Downloading Quest...");
        DownloadFile(questApp.url, questApp.output);
        SetStatus(L"Done (Quest)");
            }).detach();

    if (PtInRect(&rWindows, p))
        std::thread([] {
        SetStatus(L"Downloading Windows...");
        DownloadFile(windowsApp.url, windowsApp.output);
        SetStatus(L"Extracting...");
        system("powershell Expand-Archive -Force Windows.zip .\\Windows");
        SaveWindowsState();
        SetStatus(L"Done (Windows)");
            }).detach();

    if (windowsInstalled && PtInRect(&rLaunch, p))
    {
        ShellExecuteW(NULL, L"open",
            L".\\Windows\\Martani 360.exe",
            NULL, NULL, SW_SHOW);
    }
}

// ---------------- WINDOW ----------------
LRESULT CALLBACK WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    switch (msg)
    {
    case WM_ERASEBKGND:
        return 1; // kill flicker

    case WM_LBUTTONDOWN:
        HandleClick(LOWORD(lParam), HIWORD(lParam));
        break;

    case WM_PAINT:
    {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hWnd, &ps);

        RECT rc;
        GetClientRect(hWnd, &rc);

        // DOUBLE BUFFER
        HDC memDC = CreateCompatibleDC(hdc);
        HBITMAP memBM = CreateCompatibleBitmap(hdc, rc.right, rc.bottom);
        SelectObject(memDC, memBM);

        // background
        HBRUSH bg = CreateSolidBrush(RGB(10, 10, 10));
        FillRect(memDC, &rc, bg);
        DeleteObject(bg);

        SetTextColor(memDC, RGB(220, 220, 220));
        SetBkMode(memDC, TRANSPARENT);

        TextOutW(memDC, 20, 20, L"Martani Launcher", 17);
        TextOutW(memDC, 20, 45, statusText.c_str(), (int)statusText.size());

        DrawButton(memDC, rQuest, L"Install Quest");
        DrawButton(memDC, rWindows, L"Install Windows");
        DrawButton(memDC, rLaunch, windowsInstalled ? L"Launch" : L"Locked", windowsInstalled);

        BitBlt(hdc, 0, 0, rc.right, rc.bottom, memDC, 0, 0, SRCCOPY);

        DeleteObject(memBM);
        DeleteDC(memDC);

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