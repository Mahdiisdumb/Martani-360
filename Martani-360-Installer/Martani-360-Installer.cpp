#include "framework.h"

#include <windows.h>
#include <winhttp.h>
#include <shellapi.h>
#include <string>
#include <thread>
#include <fstream>

#pragma comment(lib, "winhttp.lib")
#pragma comment(lib, "shell32.lib")

// ---------------- STATE ----------------
HINSTANCE hInst;
HWND hWndMain;

std::wstring statusText = L"Idle";
bool windowsInstalled = false;

// ---------------- APPS ----------------
struct AppStep {
    std::wstring name;
    std::wstring url;
    std::wstring output;
    std::wstring action;
};

AppStep questApp = {
    L"Quest",
    L"https://github.com/mahdiisdumb/martani-360/releases/latest/download/Quest.apk",
    L"Quest.apk",
    L"adb"
};

AppStep windowsApp = {
    L"Windows",
    L"https://github.com/mahdiisdumb/martani-360/releases/latest/download/Windows.zip",
    L"Windows.zip",
    L"extract"
};

// ---------------- STATUS ----------------
void SetStatus(const std::wstring& s)
{
    statusText = s;
    InvalidateRect(hWndMain, NULL, TRUE);
}

// ---------------- FILE CHECK ----------------
bool FileExists(const wchar_t* path)
{
    return GetFileAttributesW(path) != INVALID_FILE_ATTRIBUTES;
}

// ---------------- LOAD WINDOWS STATE ONLY ----------------
void LoadWindowsState()
{
    windowsInstalled = FileExists(L".\\Windows\\installed.flag");
}

// ---------------- SAVE WINDOWS STATE ----------------
void SaveWindowsState()
{
    std::ofstream f(".\\Windows\\installed.flag");
    f << "1";
    f.close();

    windowsInstalled = true;
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

// ---------------- DOWNLOAD ----------------
bool DownloadFile(const std::wstring& url, const std::wstring& out)
{
    std::wstring cmd =
        L"powershell -Command Invoke-WebRequest -Uri \"" +
        url + L"\" -OutFile \"" + out + L"\"";

    return _wsystem(cmd.c_str()) == 0;
}

// ---------------- INSTALL QUEST (NO JSON NEEDED) ----------------
void InstallQuest()
{
    SetStatus(L"Downloading Quest...");

    DownloadFile(questApp.url, questApp.output);

    SetStatus(L"Waiting ADB...");
    RunADB(L"wait-for-device");

    SetStatus(L"Installing Quest APK...");
    RunADB(L"install -r Quest.apk");

    SetStatus(L"Quest installed");
}

// ---------------- INSTALL WINDOWS (TRACKED) ----------------
void InstallWindows()
{
    SetStatus(L"Downloading Windows...");

    DownloadFile(windowsApp.url, windowsApp.output);

    SetStatus(L"Extracting...");

    system("powershell Expand-Archive -Force Windows.zip .\\Windows");

    SaveWindowsState();

    SetStatus(L"Windows installed");
}

// ---------------- CLICK HANDLER ----------------
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
        ShellExecuteW(
            NULL,
            L"open",
            L".\\Windows\\Martani-360.exe",
            NULL,
            NULL,
            SW_SHOW
        );
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

        // QUEST CARD
        RECT quest = { 60, 80, 460, 170 };
        HBRUSH c1 = CreateSolidBrush(RGB(30, 30, 30));
        FillRect(hdc, &quest, c1);
        DeleteObject(c1);
        TextOutW(hdc, 80, 120, L"Install Quest APK", 18);

        // WINDOWS CARD
        RECT windows = { 60, 190, 460, 280 };
        HBRUSH c2 = CreateSolidBrush(RGB(30, 30, 30));
        FillRect(hdc, &windows, c2);
        DeleteObject(c2);
        TextOutW(hdc, 80, 230, L"Install Windows", 16);

        // LAUNCH BUTTON (ONLY IF WINDOWS INSTALLED)
        if (windowsInstalled)
        {
            RECT launch = { 60, 300, 460, 360 };

            HBRUSH lb = CreateSolidBrush(RGB(0, 80, 0));
            FillRect(hdc, &launch, lb);
            DeleteObject(lb);

            TextOutW(hdc, 170, 325, L"Launch Martani-360", 19);
        }

        // STATUS
        TextOutW(hdc, 60, 420, statusText.c_str(), (int)statusText.size());

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

    WNDCLASS wc{};
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = L"MartaniLauncher";
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);

    RegisterClass(&wc);

    hWndMain = CreateWindowW(
        wc.lpszClassName,
        L"Martani Launcher",
        WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT, CW_USEDEFAULT,
        520, 480,
        NULL, NULL,
        hInstance, NULL
    );

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