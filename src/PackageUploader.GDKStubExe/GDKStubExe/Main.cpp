//
// Main.cpp
//

#include "pch.h"
#include "Game.h"

#ifdef _GAMING_XBOX
#include <appnotify.h>
#endif

using namespace DirectX;

#ifdef __clang__
#pragma clang diagnostic ignored "-Wcovered-switch-default"
#pragma clang diagnostic ignored "-Wswitch-enum"
#endif

#pragma warning(disable : 4061)

namespace
{
    std::unique_ptr<Game> g_game;
#ifdef _GAMING_XBOX
    HANDLE g_plmSuspendComplete = nullptr;
    HANDLE g_plmSignalResume = nullptr;
#endif
}

#ifdef _GAMING_XBOX
bool g_HDRMode = false;
#endif

LRESULT CALLBACK WndProc(HWND, UINT, WPARAM, LPARAM);
#ifdef _GAMING_XBOX
void SetDisplayMode() noexcept;
#endif
void ExitGame() noexcept;

// Entry point
int WINAPI wWinMain(_In_ HINSTANCE hInstance, _In_opt_ HINSTANCE, _In_ LPWSTR lpCmdLine, _In_ int nCmdShow)
{
    UNREFERENCED_PARAMETER(lpCmdLine);

#ifndef _GAMING_XBOX
    try
    {
#endif

    if (!XMVerifyCPUSupport())
    {
#ifdef _DEBUG
        OutputDebugStringA("ERROR: This hardware does not support the required instruction set.\n");
#ifdef __AVX2__
        OutputDebugStringA("This may indicate a Gaming.Xbox.Scarlett.x64 binary is being run on an Xbox One.\n");
#endif
#endif
        return 1;
    }

    if (FAILED(XGameRuntimeInitialize()))
        return 1;

#ifdef _GAMING_XBOX
    // Microsoft GDKX supports UTF-8 everywhere
    assert(GetACP() == CP_UTF8);
#else
    // PC: Initialize COM for XAudio2
    HRESULT hr = CoInitializeEx(nullptr, COINIT_MULTITHREADED);
    if (FAILED(hr))
        return 1;
#endif

    g_game = std::make_unique<Game>();

#ifdef _GAMING_XBOX
    // Register class and create window (Xbox path with PLM)
    PAPPSTATE_REGISTRATION hPLM = {};
    PAPPCONSTRAIN_REGISTRATION hPLM2 = {};
#endif

    {
        // Register class
        WNDCLASSEXA wcex = {};
        wcex.cbSize = sizeof(WNDCLASSEXA);
        wcex.style = CS_HREDRAW | CS_VREDRAW;
        wcex.lpfnWndProc = WndProc;
        wcex.hInstance = hInstance;
        wcex.lpszClassName = u8"GDKStubExeWindowClass";
        wcex.hbrBackground = reinterpret_cast<HBRUSH>(COLOR_WINDOW + 1);
        if (!RegisterClassExA(&wcex))
            return 1;

        // Create window
        HWND hwnd = CreateWindowExA(0, u8"GDKStubExeWindowClass", u8"GDKStubExe", WS_OVERLAPPEDWINDOW,
            CW_USEDEFAULT, CW_USEDEFAULT, 1920, 1080,
            nullptr, nullptr, hInstance,
            nullptr);
        if (!hwnd)
            return 1;

        ShowWindow(hwnd, nCmdShow);

#ifdef _GAMING_XBOX
        SetDisplayMode();
#endif

        SetWindowLongPtr(hwnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(g_game.get()));

        g_game->Initialize(hwnd);

#ifdef _GAMING_XBOX
        g_plmSuspendComplete = CreateEventEx(nullptr, nullptr, 0, EVENT_MODIFY_STATE | SYNCHRONIZE);
        g_plmSignalResume = CreateEventEx(nullptr, nullptr, 0, EVENT_MODIFY_STATE | SYNCHRONIZE);
        if (!g_plmSuspendComplete || !g_plmSignalResume)
            return 1;

        if (RegisterAppStateChangeNotification([](BOOLEAN quiesced, PVOID context)
        {
            if (quiesced)
            {
                ResetEvent(g_plmSuspendComplete);
                ResetEvent(g_plmSignalResume);

                PostMessage(reinterpret_cast<HWND>(context), WM_USER, 0, 0);

                std::ignore = WaitForSingleObject(g_plmSuspendComplete, INFINITE);
            }
            else
            {
                SetEvent(g_plmSignalResume);
            }
        }, hwnd, &hPLM))
            return 1;

        if (RegisterAppConstrainedChangeNotification([](BOOLEAN constrained, PVOID context)
        {
            SendMessage(reinterpret_cast<HWND>(context), WM_USER + 1, (constrained) ? 1u : 0u, 0);
        }, hwnd, &hPLM2))
            return 1;
#endif // _GAMING_XBOX
    }

    // Main message loop
    MSG msg = {};
    while (WM_QUIT != msg.message)
    {
        if (PeekMessage(&msg, nullptr, 0, 0, PM_REMOVE))
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }
        else
        {
            g_game->Tick();
        }
    }

    g_game.reset();

#ifdef _GAMING_XBOX
    UnregisterAppStateChangeNotification(hPLM);
    UnregisterAppConstrainedChangeNotification(hPLM2);

    CloseHandle(g_plmSuspendComplete);
    CloseHandle(g_plmSignalResume);

    XGameRuntimeUninitialize();
#else
    CoUninitialize();
    XGameRuntimeUninitialize();
#endif

    return static_cast<int>(msg.wParam);

#ifndef _GAMING_XBOX
    } // try
    catch (const std::exception& e)
    {
        OutputDebugStringA("FATAL: ");
        OutputDebugStringA(e.what());
        OutputDebugStringA("\n");
        MessageBoxA(nullptr, e.what(), "GDKStubExe - Fatal Error", MB_OK | MB_ICONERROR);
        return 1;
    }
#endif
}

// Windows procedure
LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    auto game = reinterpret_cast<Game*>(GetWindowLongPtr(hWnd, GWLP_USERDATA));

    switch (message)
    {
#ifdef _GAMING_XBOX
    case WM_USER:
        if (game)
        {
            game->OnSuspending();

            SetEvent(g_plmSuspendComplete);

            std::ignore = WaitForSingleObject(g_plmSignalResume, INFINITE);

            SetDisplayMode();

            game->OnResuming();
        }
        break;

    case WM_USER + 1:
        if (game)
        {
            if (wParam)
            {
                game->OnConstrained();
            }
            else
            {
                SetDisplayMode();

                game->OnUnConstrained();
            }
        }
        break;
#else // PC message handling
    case WM_ACTIVATEAPP:
        if (game)
        {
            if (wParam)
                game->OnResuming();
            else
                game->OnSuspending();
        }
        break;

    case WM_DESTROY:
        PostQuitMessage(0);
        break;
#endif

    default:
        break;
    }

    return DefWindowProc(hWnd, message, wParam, lParam);
}

#ifdef _GAMING_XBOX
// HDR helper
void SetDisplayMode() noexcept
{
    if (g_game && g_game->RequestHDRMode())
    {
        auto result = XDisplayTryEnableHdrMode(XDisplayHdrModePreference::PreferHdr, nullptr);

        g_HDRMode = (result == XDisplayHdrModeResult::Enabled);

#ifdef _DEBUG
        OutputDebugStringA((g_HDRMode) ? "INFO: Display in HDR Mode\n" : "INFO: Display in SDR Mode\n");
#endif
    }
}
#endif

// Exit helper
void ExitGame() noexcept
{
    PostQuitMessage(0);
}
