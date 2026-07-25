#include "framework.h"
#include <windows.h>
#include <winhttp.h>
#include <wininet.h>
#include <shellapi.h>
#include <commctrl.h>
#include <string>
#include <thread>
#include <fstream>
#include <cstdio>
#include <cstdlib>
#include <cmath>
#include <shlobj.h>
#include <deque>
#include <random>
#include <sstream>
#include <iomanip>

#pragma comment(lib, "winhttp.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "wininet.lib")
#pragma comment(lib, "comctl32.lib")

#include "resource.h"

// =====================================================================
//  HIGHER PROPERTY SURVEY PROGRAM  --  presentation layer
//  Backend logic (download / ADB install / extraction / launch) is
//  UNCHANGED from the original build. Only rendering, text, and
//  animation are new.
// =====================================================================

// ---------------- STATE (backend, unchanged) ----------------
HINSTANCE hInst;
HWND hWndMain;

std::wstring statusText = L"Idle";
std::wstring errorText = L"";
bool windowsInstalled = false;

double downloadCurrentMB = 0;
double downloadTotalMB = 0;

std::wstring QUEST_PACKAGE = L"com.MahdiStudios.Martani360";

// Embedded ADB path
std::wstring adbTempFolder = L"";
std::wstring adbExePath = L"";

// ---------------- THEME STATE (new) ----------------
#define COL_BG          RGB(0, 0, 0)
#define COL_GRID_DIM    RGB(0, 28, 10)
#define COL_GREEN       RGB(60, 255, 120)
#define COL_GREEN_DIM   RGB(0, 150, 60)
#define COL_GREEN_FAINT RGB(0, 80, 30)
#define COL_RED         RGB(255, 70, 70)
#define COL_RED_DIM     RGB(140, 20, 20)
#define COL_SCANLINE    RGB(0, 14, 6)

HFONT hFontMain = NULL;
HFONT hFontSmall = NULL;
HFONT hFontHeader = NULL;

int gridOffset = 0;
long tickCount = 0;
bool flickerActive = false;
int flickerTimer = 0;
int currentProgressPercent = 0;
long anomalyFlashUntilTick = 0;

std::wstring tickerText;
int tickerTextWidth = 0;
int tickerOffset = 0;

struct LogEntry { std::wstring text; bool anomaly; };
std::deque<LogEntry> logLines;
const size_t MAX_LOG_LINES = 16;

std::wstring resonanceValue = L"Stable";
std::wstring dissonanceValue = L"0.000%";
std::wstring saveStateValue = L"Unbound";
std::wstring vesselValue = L"Unknown";
std::wstring hplinkValue = L"Disconnected";

std::mt19937 g_rng((unsigned)GetTickCount64());

static const wchar_t* idleMessages[] = {
    L"Searching for compatible vessels...",
    L"Save State generator idle...",
    L"Listening on Resonance channel...",
    L"No Dissonance detected.",
    L"Higher Property Link dormant.",
    L"Vessel telemetry nominal.",
    L"Awaiting operator input...",
    L"Ambient Resonance within tolerance.",
    L"Standing by.",
    L"Field integrity holding.",
    L"Cross-referencing Vessel registry...",
    L"Survey grid stable.",
    L"Head signature: dormant.",
    L"Contagions detected as none.",
    L"No failed vessels detected nearby.",
    L"Martani cognition trace: within bounds."
};

static const wchar_t* anomalyBurst[] = {
    L"ARCHIVE FRAGMENT SURFACED",
    L"record: resonance dissonance works correctly at this moment",
    L"Nural interface: operational",
    L"XRI SDK's for usr HMD:working",
    L"martani health status: stable",
    L"non-Martani vessels cannot create Save States",
    L"failed vessels terminate the user session on bind",
    L"only Martani-class signatures persist a Save State",
    L"If Rejected Resonance: grasp corrupts, Dissonance recursive",
    L"Dissonance-within-Dissonance event recorded",
    L"emergency egress indexed:terminate vessel we will recover it",
    L"advisory: do not approach anything vessel isnt invinciable to",
    L"archive fragment closed."
};

static const wchar_t* tickerFragments[] = {
    L"HIGHER PROPERTY SURVEY PROGRAM",
    L"RESONANCE DISSONANCE MONITORING ACTIVE",
    L"VESSEL INTEGRITY: NOMINAL",
    L"DO NOT NULLIFY DURING BINDING",
    L"MARTANI CLASS VESSEL NON CLASSIFIED",
    L"DO WHAT YOU WANT",
    L"MARTANI SIGNATURE REQUIRED FOR SAVE STATE GENERATION",
    L"IF ERR PLESAE TERMIANTE SESSION OR USER DEAF STATE IS PROBABLE"
};

// ---------------- APPS (backend, unchanged) ----------------
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

// ---------------- LAYOUT ----------------
RECT rHeader = { 20,  15, 860, 100 };
RECT rStatusPanel = { 20, 112, 860, 232 };
RECT rQuest = { 40, 252, 420, 312 };
RECT rWindows = { 460, 252, 840, 312 };
RECT rLaunch = { 40, 324, 840, 374 };
RECT rProgressArea = { 40, 396, 840, 412 };
RECT rLog = { 20, 432, 860, 650 };
RECT rTicker = { 20, 658, 860, 684 };

// ---------------- LOG / STATUS HELPERS ----------------
void LogMsg(const std::wstring& msg, bool anomaly = false)
{
    SYSTEMTIME st; GetLocalTime(&st);
    wchar_t buf[16];
    swprintf_s(buf, L"[%02d:%02d:%02d] ", st.wHour, st.wMinute, st.wSecond);
    logLines.push_back({ std::wstring(buf) + msg, anomaly });
    while (logLines.size() > MAX_LOG_LINES) logLines.pop_front();
    if (hWndMain) InvalidateRect(hWndMain, NULL, FALSE);
}

void TriggerAnomaly()
{
    for (auto* line : anomalyBurst)
        LogMsg(line, true);
    resonanceValue = L"Unstable";
    anomalyFlashUntilTick = tickCount + 90;
}

void SetStatus(const std::wstring& s)
{
    statusText = s;
    errorText = L""; // Clear error when setting new status
    LogMsg(s);
    if (hWndMain) InvalidateRect(hWndMain, NULL, FALSE);
}

void SetError(const std::wstring& e)
{
    errorText = e;
    resonanceValue = L"Unstable";
    LogMsg(L"ERROR :: " + e, true);
    if (hWndMain) InvalidateRect(hWndMain, NULL, FALSE);
}

void SetProgress(int p)
{
    currentProgressPercent = p;
    if (hWndMain) InvalidateRect(hWndMain, NULL, FALSE);
}

// ---------------- FILE (backend, unchanged) ----------------
bool FileExists(const wchar_t* path)
{
    return GetFileAttributesW(path) != INVALID_FILE_ATTRIBUTES;
}

// ---------------- RESOURCE EXTRACTION (backend, unchanged) ----------------
bool ExtractResourceToFile(int resourceId, const wchar_t* resourceType, const wchar_t* outputPath)
{
    HRSRC hResource = FindResourceW(hInst, MAKEINTRESOURCE(resourceId), resourceType);
    if (!hResource)
        return false;

    HGLOBAL hGlobal = LoadResource(hInst, hResource);
    if (!hGlobal)
        return false;

    DWORD dwSize = SizeofResource(hInst, hResource);
    LPVOID lpData = LockResource(hGlobal);
    if (!lpData || dwSize == 0)
        return false;

    // Create directory if needed
    wchar_t dirPath[MAX_PATH];
    wcscpy_s(dirPath, outputPath);
    wchar_t* lastBackslash = wcsrchr(dirPath, L'\\');
    if (lastBackslash)
    {
        *lastBackslash = L'\0';
        CreateDirectoryW(dirPath, NULL);
    }

    // Write to file
    HANDLE hFile = CreateFileW(outputPath, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hFile == INVALID_HANDLE_VALUE)
        return false;

    DWORD dwBytesWritten;
    BOOL success = WriteFile(hFile, lpData, dwSize, &dwBytesWritten, NULL);
    CloseHandle(hFile);

    return success && dwBytesWritten == dwSize;
}

bool InitializeEmbeddedADB()
{
    // Get temp folder
    wchar_t tempPath[MAX_PATH];
    if (!GetTempPathW(MAX_PATH, tempPath))
        return false;

    // Create ADB subfolder
    wcscat_s(tempPath, MAX_PATH, L"MartaniADB\\");
    adbTempFolder = tempPath;

    // Create the folder
    CreateDirectoryW(adbTempFolder.c_str(), NULL);

    // Extract ADB files
    wchar_t adbExeFile[MAX_PATH];
    wcscpy_s(adbExeFile, adbTempFolder.c_str());
    wcscat_s(adbExeFile, MAX_PATH, L"adb.exe");

    wchar_t adbDll1File[MAX_PATH];
    wcscpy_s(adbDll1File, adbTempFolder.c_str());
    wcscat_s(adbDll1File, MAX_PATH, L"AdbWinApi.dll");

    wchar_t adbDll2File[MAX_PATH];
    wcscpy_s(adbDll2File, adbTempFolder.c_str());
    wcscat_s(adbDll2File, MAX_PATH, L"AdbWinUsbApi.dll");

    // NOTE: these are pulled from the RC's "BINARY" resource sector,
    // not RT_RCDATA. FindResourceW resolves "BINARY" as a user-defined
    // resource-type string because it isn't one of the predefined RT_*
    // constants -- your .rc entries must declare these three with the
    // BINARY keyword, e.g.:
    //   IDR_ADB_EXE   BINARY   "adb.exe"
    //   IDR_ADB_DLL1  BINARY   "AdbWinApi.dll"
    //   IDR_ADB_DLL2  BINARY   "AdbWinUsbApi.dll"

    // Extract only if not already present
    if (!FileExists(adbExeFile))
    {
        if (!ExtractResourceToFile(IDR_ADB_EXE, L"BINARY", adbExeFile))
            return false;
    }

    if (!FileExists(adbDll1File))
    {
        if (!ExtractResourceToFile(IDR_ADB_DLL1, L"BINARY", adbDll1File))
            return false;
    }

    if (!FileExists(adbDll2File))
    {
        if (!ExtractResourceToFile(IDR_ADB_DLL2, L"BINARY", adbDll2File))
            return false;
    }

    adbExePath = adbExeFile;
    return true;
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

// ---------------- ADB HELPERS (backend, unchanged) ----------------
bool IsAdbAvailable()
{
    if (adbExePath.empty())
        return false;

    char command[512];
    size_t convertedChars = 0;
    wcstombs_s(&convertedChars, command, 512, adbExePath.c_str(), 512);

    char fullCommand[1024];
    sprintf_s(fullCommand, "\"%s\" version > nul 2>&1", command);

    int result = system(fullCommand);
    return result == 0;
}

bool CheckAdbPermission()
{
    if (adbExePath.empty())
        return false;

    char command[512];
    size_t convertedChars = 0;
    wcstombs_s(&convertedChars, command, 512, adbExePath.c_str(), 512);

    char fullCommand[1024];
    sprintf_s(fullCommand, "\"%s\" devices 2>&1", command);

    FILE* pipe = _popen(fullCommand, "r");
    if (pipe == NULL)
        return false;

    char buffer[512];
    bool hasDevices = false;
    int lineCount = 0;

    while (fgets(buffer, sizeof(buffer), pipe) != NULL && lineCount < 20)
    {
        lineCount++;
        std::string line(buffer);

        if (line.find("List of attached devices") != std::string::npos)
        {
            continue;
        }

        size_t tabPos = line.find("\t");
        if (tabPos != std::string::npos)
        {
            std::string serial = line.substr(0, tabPos);
            std::string status = line.substr(tabPos);

            if (status.find("device") != std::string::npos &&
                status.find("offline") == std::string::npos &&
                !serial.empty() && serial != "\r" && serial != "\n")
            {
                hasDevices = true;
                break;
            }
        }
    }

    _pclose(pipe);
    return hasDevices;
}

bool InstallAPK(const std::wstring& apkPath)
{
    if (adbExePath.empty())
        return false;

    char adbCmd[512];
    size_t convertedChars = 0;
    wcstombs_s(&convertedChars, adbCmd, 512, adbExePath.c_str(), 512);

    char apkCmd[512];
    convertedChars = 0;
    wcstombs_s(&convertedChars, apkCmd, 512, apkPath.c_str(), 512);

    char fullCommand[1024];
    sprintf_s(fullCommand, "\"%s\" install -r \"%s\" 2>&1", adbCmd, apkCmd);

    FILE* pipe = _popen(fullCommand, "r");
    if (pipe == NULL)
        return false;

    char buffer[512];
    bool success = false;

    while (fgets(buffer, sizeof(buffer), pipe) != NULL)
    {
        std::string line(buffer);
        if (line.find("Success") != std::string::npos)
        {
            success = true;
            break;
        }
    }

    int status = _pclose(pipe);

    return status == 0 && success;
}

// ---------------- DOWNLOAD (backend, unchanged) ----------------
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

// ==================== DRAWING (presentation, new) ====================

std::wstring GlitchText(const std::wstring& s, double prob)
{
    static const wchar_t glyphs[] = L"#$%&*+=~^|/\\<>01";
    std::wstring out = s;
    for (auto& c : out)
    {
        if (c != L' ' && (g_rng() % 1000) < (int)(prob * 1000))
            c = glyphs[g_rng() % (sizeof(glyphs) / sizeof(wchar_t) - 1)];
    }
    return out;
}

void DrawGlowBorder(HDC hdc, RECT rc, COLORREF color)
{
    for (int i = 3; i >= 1; i--)
    {
        RECT r2 = { rc.left - i, rc.top - i, rc.right + i, rc.bottom + i };
        int fade = 4 - i;
        BYTE cr = (BYTE)GetRValue(color);
        BYTE cg = (BYTE)GetGValue(color);
        BYTE cb = (BYTE)GetBValue(color);
        HPEN pen = CreatePen(PS_SOLID, 1, RGB(cr * fade / 6, cg * fade / 6, cb * fade / 6));
        HPEN old = (HPEN)SelectObject(hdc, pen);
        HBRUSH oldBrush = (HBRUSH)SelectObject(hdc, GetStockObject(NULL_BRUSH));
        Rectangle(hdc, r2.left, r2.top, r2.right, r2.bottom);
        SelectObject(hdc, oldBrush);
        SelectObject(hdc, old);
        DeleteObject(pen);
    }
    HPEN pen = CreatePen(PS_SOLID, 1, color);
    HPEN old = (HPEN)SelectObject(hdc, pen);
    HBRUSH oldBrush = (HBRUSH)SelectObject(hdc, GetStockObject(NULL_BRUSH));
    Rectangle(hdc, rc.left, rc.top, rc.right, rc.bottom);
    SelectObject(hdc, oldBrush);
    SelectObject(hdc, old);
    DeleteObject(pen);
}

void DrawGrid(HDC hdc, RECT rc)
{
    HPEN pen = CreatePen(PS_SOLID, 1, COL_GRID_DIM);
    HPEN old = (HPEN)SelectObject(hdc, pen);

    int cx = rc.right / 2;
    int vpY = rc.bottom / 4;
    int jitter = (g_rng() % 100 == 0) ? (int)(g_rng() % 5) - 2 : 0; // rare horizontal jitter

    for (int x = -40; x <= rc.right + 40; x += 44)
    {
        MoveToEx(hdc, x + jitter, rc.bottom, NULL);
        LineTo(hdc, cx + (x - cx) / 6 + jitter, vpY);
    }

    int span = rc.bottom - vpY;
    if (span < 1) span = 1;
    for (int i = 0; i < 40; i++)
    {
        int y = rc.bottom - ((i * 20 + gridOffset) % span);
        if (y < vpY) continue;
        MoveToEx(hdc, 0, y, NULL);
        LineTo(hdc, rc.right, y);
    }

    SelectObject(hdc, old);
    DeleteObject(pen);
}

void DrawScanlines(HDC hdc, RECT rc)
{
    HPEN pen = CreatePen(PS_SOLID, 1, COL_SCANLINE);
    HPEN old = (HPEN)SelectObject(hdc, pen);
    for (int y = 0; y < rc.bottom; y += 2)
    {
        MoveToEx(hdc, 0, y, NULL);
        LineTo(hdc, rc.right, y);
    }
    SelectObject(hdc, old);
    DeleteObject(pen);
}

void DrawButton(HDC hdc, RECT rc, const std::wstring& text, COLORREF pulseColor, bool enabled = true)
{
    DrawGlowBorder(hdc, rc, enabled ? pulseColor : COL_GREEN_FAINT);
    SetBkMode(hdc, TRANSPARENT);
    SetTextColor(hdc, enabled ? COL_GREEN : COL_GREEN_FAINT);
    SelectObject(hdc, hFontMain);
    RECT textRc = rc;
    DrawTextW(hdc, text.c_str(), -1, &textRc, DT_CENTER | DT_VCENTER | DT_SINGLELINE);
}

// ---------------- CLICK ----------------
void HandleClick(int x, int y)
{
    POINT p = { x, y };

    if (PtInRect(&rQuest, p))
        std::thread([] {
        hplinkValue = L"Scanning";
        vesselValue = L"Searching";
        SetStatus(L"Scanning for Martani vessels...");

        if (!IsAdbAvailable())
        {
            hplinkValue = L"Disconnected";
            SetError(L"No vessel bridge detected.");
            SetStatus(L"Vessel deployment failed.");
            return;
        }

        hplinkValue = L"Bridge Detected";
        LogMsg(L"Martani signature detected.");

        if (!CheckAdbPermission())
        {
            SetError(L"The vessel rejected the Resonance handshake.");
            SetStatus(L"Vessel deployment failed.");
            return;
        }

        hplinkValue = L"Linked";
        vesselValue = L"VISOR Vessel";
        SetStatus(L"Synchronizing with Higher Property...");
        LogMsg(L"Binding Resonance...");

        if (!DownloadFile(questApp.url, questApp.output))
        {
            SetError(L"Synchronization with Higher Property failed.");
            SetStatus(L"Vessel deployment failed.");
            return;
        }

        SetStatus(L"Binding vessel to Resonance...");
        LogMsg(L"Materializing Vessel...");

        if (!InstallAPK(questApp.output))
        {
            SetError(L"The selected vessel generated Dissonance.");
            SetStatus(L"Vessel deployment failed.");
            return;
        }

        saveStateValue = L"Bound";
        resonanceValue = L"Stable";
        LogMsg(L"Save State anchor established.");
        LogMsg(L"Vessel synchronized.");
        SetStatus(L"Persistent Save State established.");
            }).detach();

    if (PtInRect(&rWindows, p))
        std::thread([] {
        vesselValue = L"GPU Vessel";
        SetStatus(L"Synchronizing with Higher Property...");
        DownloadFile(windowsApp.url, windowsApp.output);
        SetStatus(L"Materializing Vessel...");
        system("powershell Expand-Archive -Force Windows.zip .\\Windows");
        SaveWindowsState();
        saveStateValue = L"Bound";
        LogMsg(L"GPU Vessel materialized.");
        SetStatus(L"Persistent Save State established.");
            }).detach();

    if (windowsInstalled && PtInRect(&rLaunch, p))
    {
        LogMsg(L"Resuming Session...");
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
    case WM_CREATE:
    {
        hFontMain = CreateFontW(16, 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE,
            DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS,
            DEFAULT_QUALITY, FIXED_PITCH | FF_MODERN, L"Cascadia Mono");
        hFontSmall = CreateFontW(14, 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE,
            DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS,
            DEFAULT_QUALITY, FIXED_PITCH | FF_MODERN, L"Cascadia Mono");
        hFontHeader = CreateFontW(20, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE,
            DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS,
            DEFAULT_QUALITY, FIXED_PITCH | FF_MODERN, L"Cascadia Mono");

        // build ticker text + measure its width once
        std::wstring t;
        for (auto* frag : tickerFragments) { t += frag; t += L"     //     "; }
        tickerText = t;
        {
            HDC tdc = GetDC(hWnd);
            HFONT oldF = (HFONT)SelectObject(tdc, hFontSmall);
            SIZE sz; GetTextExtentPoint32W(tdc, tickerText.c_str(), (int)tickerText.size(), &sz);
            tickerTextWidth = sz.cx;
            SelectObject(tdc, oldF);
            ReleaseDC(hWnd, tdc);
        }

        LogMsg(L"Initializing Higher Property...");
        SetTimer(hWnd, 1, 66, NULL);
    }
    break;

    case WM_ERASEBKGND:
        return 1; // kill flicker from default background erase

    case WM_LBUTTONDOWN:
        HandleClick(LOWORD(lParam), HIWORD(lParam));
        break;

    case WM_TIMER:
    {
        tickCount++;
        gridOffset = (gridOffset + 2) % 10000;
        tickerOffset = (tickerOffset + 2) % (tickerTextWidth > 0 ? tickerTextWidth : 1);

        if (g_rng() % 90 == 0)
        {
            flickerActive = true;
            flickerTimer = 2;
        }
        if (flickerTimer > 0)
        {
            flickerTimer--;
            if (flickerTimer == 0) flickerActive = false;
        }

        // rare corrupted-transmission burst
        if (g_rng() % 900 == 0)
            TriggerAnomaly();

        // recover resonance once the anomaly window passes, unless a real error is active
        if (anomalyFlashUntilTick > 0 && tickCount > anomalyFlashUntilTick && errorText.empty())
        {
            resonanceValue = L"Stable";
            anomalyFlashUntilTick = 0;
        }

        // cosmetic dissonance jitter while idle
        {
            double jitter = (g_rng() % 100) / 100000.0;
            std::wostringstream oss;
            oss << std::fixed << std::setprecision(3) << jitter << L"%";
            dissonanceValue = oss.str();
        }

        if (tickCount % 90 == 0)
        {
            LogMsg(idleMessages[g_rng() % (sizeof(idleMessages) / sizeof(idleMessages[0]))]);
        }

        InvalidateRect(hWnd, NULL, FALSE);
    }
    break;

    case WM_PAINT:
    {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hWnd, &ps);

        RECT rc;
        GetClientRect(hWnd, &rc);

        HDC memDC = CreateCompatibleDC(hdc);
        HBITMAP memBM = CreateCompatibleBitmap(hdc, rc.right, rc.bottom);
        SelectObject(memDC, memBM);

        HBRUSH bg = CreateSolidBrush(COL_BG);
        FillRect(memDC, &rc, bg);
        DeleteObject(bg);

        DrawGrid(memDC, rc);
        DrawScanlines(memDC, rc);

        SetBkMode(memDC, TRANSPARENT);

        bool inAnomaly = anomalyFlashUntilTick > tickCount;

        // pulsing glow color (breathing sine wave), swaps to red pulse during an anomaly
        double pulse = (sin(tickCount * 0.07) + 1.0) / 2.0; // 0..1
        COLORREF pulseColor = inAnomaly
            ? RGB(180 + (BYTE)(pulse * 60), 30, 30)
            : RGB(0, (BYTE)(140 + pulse * 115), (BYTE)(40 + pulse * 40));

        // -------- header --------
        DrawGlowBorder(memDC, rHeader, pulseColor);
        SelectObject(memDC, hFontHeader);
        SetTextColor(memDC, inAnomaly ? COL_RED : COL_GREEN);
        {
            RECT headerText = { rHeader.left + 15, rHeader.top + 6, rHeader.right - 15, rHeader.bottom - 6 };
            std::wstring headerStr = L"HIGHER PROPERTY\nRESONANCE DISSONANCE\nSURVEY PROGRAM   |   Build 4.03";
            if (flickerActive)
            {
                // chromatic-aberration style ghost duplicate
                SetTextColor(memDC, COL_RED_DIM);
                RECT ghost = headerText; ghost.left += 2; ghost.top += 1;
                DrawTextW(memDC, headerStr.c_str(), -1, &ghost, DT_LEFT | DT_TOP);
                SetTextColor(memDC, inAnomaly ? COL_RED : COL_GREEN);
            }
            DrawTextW(memDC, headerStr.c_str(), -1, &headerText, DT_LEFT | DT_TOP);
        }

        // -------- status panel --------
        DrawGlowBorder(memDC, rStatusPanel, pulseColor);
        SelectObject(memDC, hFontMain);
        {
            int px = rStatusPanel.left + 20;
            int py = rStatusPanel.top + 10;
            int lineH = 19;

            auto drawStat = [&](const wchar_t* label, const std::wstring& val, COLORREF valColor)
                {
                    SetTextColor(memDC, COL_GREEN_DIM);
                    TextOutW(memDC, px, py, label, (int)wcslen(label));
                    SetTextColor(memDC, valColor);
                    TextOutW(memDC, px + 230, py, val.c_str(), (int)val.size());
                    py += lineH;
                };

            drawStat(L"RESONANCE", resonanceValue, resonanceValue == L"Stable" ? COL_GREEN : COL_RED);
            drawStat(L"DISSONANCE", dissonanceValue, COL_GREEN);
            drawStat(L"SAVE STATE", saveStateValue, COL_GREEN);
            drawStat(L"VESSEL", vesselValue, COL_GREEN);
            drawStat(L"HIGHER PROPERTY LINK", hplinkValue, COL_GREEN);
        }

        // -------- buttons --------
        DrawButton(memDC, rQuest, L"Deploy VISOR Vessel", pulseColor);
        DrawButton(memDC, rWindows, L"Deploy GPU Vessel", pulseColor);
        DrawButton(memDC, rLaunch, windowsInstalled ? L"Resume Session" : L"Vessel Unavailable", pulseColor, windowsInstalled);

        // -------- status / error line --------
        SelectObject(memDC, hFontSmall);
        SetTextColor(memDC, COL_GREEN);
        {
            RECT statusLineRc = { rProgressArea.left, rProgressArea.top - 20, rProgressArea.right, rProgressArea.top - 2 };
            TextOutW(memDC, statusLineRc.left, statusLineRc.top, statusText.c_str(), (int)statusText.size());
        }
        if (!errorText.empty())
        {
            SetTextColor(memDC, COL_RED);
            std::wstring errFull = L"ERROR :: " + errorText;
            TextOutW(memDC, rStatusPanel.left + 400, rStatusPanel.top + 10, errFull.c_str(), (int)errFull.size());
        }

        // -------- progress bar --------
        DrawGlowBorder(memDC, rProgressArea, COL_GREEN_FAINT);
        {
            RECT fillRc = rProgressArea;
            int w = rProgressArea.right - rProgressArea.left;
            fillRc.right = fillRc.left + (int)(w * (currentProgressPercent / 100.0));
            HBRUSH fillBrush = CreateSolidBrush(COL_GREEN_DIM);
            FillRect(memDC, &fillRc, fillBrush);
            DeleteObject(fillBrush);
        }

        // -------- log panel --------
        DrawGlowBorder(memDC, rLog, inAnomaly ? pulseColor : COL_GREEN_FAINT);
        SelectObject(memDC, hFontSmall);
        {
            int ly = rLog.top + 8;
            size_t idx = 0;
            for (auto& entry : logLines)
            {
                bool isLast = (idx == logLines.size() - 1);
                SetTextColor(memDC, entry.anomaly ? COL_RED : COL_GREEN_DIM);
                std::wstring lineText = (flickerActive && isLast) ? GlitchText(entry.text, 0.12) : entry.text;
                TextOutW(memDC, rLog.left + 12, ly, lineText.c_str(), (int)lineText.size());
                ly += 16;
                idx++;
            }
        }

        // -------- ticker tape --------
        DrawGlowBorder(memDC, rTicker, COL_GREEN_FAINT);
        SetTextColor(memDC, COL_GREEN_DIM);
        {
            HRGN clipRgn = CreateRectRgn(rTicker.left + 2, rTicker.top + 2, rTicker.right - 2, rTicker.bottom - 2);
            SelectClipRgn(memDC, clipRgn);
            int ty = rTicker.top + (rTicker.bottom - rTicker.top) / 2 - 8;
            int loopW = tickerTextWidth > 0 ? tickerTextWidth : 1;
            TextOutW(memDC, rTicker.left - tickerOffset, ty, tickerText.c_str(), (int)tickerText.size());
            TextOutW(memDC, rTicker.left - tickerOffset + loopW, ty, tickerText.c_str(), (int)tickerText.size());
            SelectClipRgn(memDC, NULL);
            DeleteObject(clipRgn);
        }

        // -------- flicker glitch bar --------
        if (flickerActive)
        {
            HBRUSH flickerBrush = CreateSolidBrush(RGB(0, 0, 0));
            RECT glitchRc = { 0, (int)(g_rng() % (rc.bottom > 0 ? rc.bottom : 1)), rc.right, 0 };
            glitchRc.bottom = glitchRc.top + 3;
            FillRect(memDC, &glitchRc, flickerBrush);
            DeleteObject(flickerBrush);
        }

        BitBlt(hdc, 0, 0, rc.right, rc.bottom, memDC, 0, 0, SRCCOPY);

        DeleteObject(memBM);
        DeleteDC(memDC);

        EndPaint(hWnd, &ps);
    }
    break;

    case WM_DESTROY:
        KillTimer(hWnd, 1);
        if (hFontMain) DeleteObject(hFontMain);
        if (hFontSmall) DeleteObject(hFontSmall);
        if (hFontHeader) DeleteObject(hFontHeader);
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

    if (!InitializeEmbeddedADB())
    {
        MessageBoxW(NULL, L"Failed to initialize the vessel bridge. The Survey Program may not function correctly.",
            L"Initialization Error", MB_ICONWARNING);
    }

    INITCOMMONCONTROLSEX icc{};
    icc.dwSize = sizeof(icc);
    icc.dwICC = ICC_PROGRESS_CLASS;
    InitCommonControlsEx(&icc);

    WNDCLASS wc{};
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = L"HigherPropertySurveyProgram";
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)GetStockObject(BLACK_BRUSH);

    RegisterClass(&wc);

    hWndMain = CreateWindowW(wc.lpszClassName,
        L"HIGHER PROPERTY SURVEY PROGRAM",
        WS_OVERLAPPEDWINDOW & ~WS_THICKFRAME & ~WS_MAXIMIZEBOX,
        CW_USEDEFAULT, CW_USEDEFAULT,
        900, 800,
        NULL, NULL,
        hInstance, NULL);

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