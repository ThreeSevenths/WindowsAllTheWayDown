using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Kernel32;
using Vanara;
using System.Diagnostics.CodeAnalysis;

namespace WindowsAllTheWayDown
{
#nullable disable

    [SupportedOSPlatform("windows")]
    internal static class Program
    {
        static SafeHBRUSH hBrush;
        const int DIVISIONS = 5;
        const string szChildClass = "WindowsAllTheWayDown_Child";

        [RequiresAssemblyFiles("Calls System.Runtime.InteropServices.Marshal.GetHINSTANCE(Module)")]
        static int Main(string[] args)
        {
            SafeHINSTANCE hinstance = new((HINSTANCE)Marshal.GetHINSTANCE(typeof(Program).Module), ownsHandle: false);

            WNDCLASS wndclass = new();

            var szAppName = "WindowsAllTheWayDown";


            wndclass.style = WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW;
            wndclass.lpfnWndProc = WndProc;
            wndclass.hInstance = hinstance;
            wndclass.hIcon = LoadIcon(HINSTANCE.NULL, IDI_APPLICATION);
            wndclass.hCursor = LoadCursor(HINSTANCE.NULL, IDC_ARROW);
            wndclass.hbrBackground = HBRUSH.NULL;
            wndclass.lpszMenuName = null!;
            wndclass.lpszClassName = szAppName;

            var windowatom = RegisterClass(wndclass);

            if (windowatom == ATOM.INVALID_ATOM)
            {
                MessageBox(HWND.NULL, "This program requires Windows NT!", szAppName, MB_FLAGS.MB_ICONERROR);
            }

            wndclass.lpfnWndProc = ChildWndProc;
            wndclass.cbWndExtra = sizeof(long);
            wndclass.hIcon = HICON.NULL;
            wndclass.lpszClassName = szChildClass;

            var childatom = RegisterClass(wndclass);

            SafeHWND hwnd = CreateWindow(
                szAppName,
                "Windows All The Way Down",
                WindowStyles.WS_OVERLAPPEDWINDOW,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                HWND.NULL,
                HMENU.NULL,
                hinstance,
                nint.Zero
                );

            hBrush = CreateSolidBrush(new COLORREF(255, 255, 255));


            try
            {

                ShowWindow(hwnd, ShowWindowCommand.SW_NORMAL);

                UpdateWindow(hwnd);

                MSG msg;

                while ((BOOL)GetMessage(out msg, HWND.NULL, 0, 0))
                {
                    TranslateMessage(msg);
                    DispatchMessage(msg);
                }

                return msg.wParam.ToInt32();

            }
            finally
            {
                for (var x = 0; x < DIVISIONS; x++)
                    for (var y = 0; y < DIVISIONS; y++)
                        if (hWndChild[x, y].IsInvalid == false)
                            DestroyWindow(hWndChild[x, y]);

                hwnd?.Dispose();
                
                hBrush?.Dispose();
                hMenu?.Dispose();

                UnregisterClass(szChildClass, hinstance);
                UnregisterClass(szAppName, hinstance);

                hinstance?.Dispose();
            }

        }

        static int cxClient, cyClient;
        static HWND[,] hWndChild = new HWND[DIVISIONS, DIVISIONS];
        static SafeHMENU hMenu;

        static nint WndProc(HWND hWnd, uint msg, nint wParam, nint lParam)
        {
            int cxBlock, cyBlock, x, y;


            switch ((WindowMessage)msg)
            {
                case WindowMessage.WM_CREATE:

                    hMenu = CreateAppMenu(hWnd);

                    SetMenu(hWnd, hMenu);

                    var cstruct = Marshal.PtrToStructure<CREATESTRUCT>(lParam);

                    for (x = 0; x < DIVISIONS; x++)
                        for (y = 0; y < DIVISIONS; y++)
                            hWndChild[x, y] = CreateWindow(szChildClass, null,
                                WindowStyles.WS_CHILDWINDOW | WindowStyles.WS_VISIBLE,
                                0, 0, 0, 0, hWnd, (HMENU)(y << 8 | x),
                                cstruct.hInstance, nint.Zero);
                    return 0;
                case WindowMessage.WM_SIZE:
                    cxClient = Macros.LOWORD(lParam);
                    cyClient = Macros.HIWORD(lParam);

                    cxBlock = cxClient / DIVISIONS;
                    cyBlock = cyClient / DIVISIONS;

                    for (x = 0; x < DIVISIONS; x++)
                        for (y = 0; y < DIVISIONS; y++)
                            MoveWindow(hWndChild[x, y], x * cxBlock, y * cyBlock, cxBlock, cyBlock, true);

                    return 0;
                case WindowMessage.WM_COMMAND:

                    switch ((nint)Macros.LOWORD(wParam))
                    {
                        case IDM_FILE_QUIT:
                            PostMessage(hWnd, WindowMessage.WM_QUIT, nint.Zero, nint.Zero);
                            break;
                        case IDM_BACKGROUND_GRAY:
                            hBrush?.Dispose();
                            hBrush = CreateSolidBrush(new COLORREF(128, 128, 128));
                            InvalidateRect(hWnd, null, bErase: true);
                            break;
                        case IDM_BACKGROUND_WHITE:
                            hBrush?.Dispose();
                            hBrush = CreateSolidBrush(new COLORREF(255, 255, 255));
                            InvalidateRect(hWnd, null, bErase: true);
                            break;
                        case IDM_HELP_ABOUT:
                            MessageBox(hWnd, "Windows All The Way Down\nAn application of the Win32 API in C#", "Windows All The Way Down", MB_FLAGS.MB_OK | MB_FLAGS.MB_ICONINFORMATION);
                            break;
                    }
                    return 0;
                case WindowMessage.WM_LBUTTONDOWN:
                    MessageBeep(0);

                    return 0;
                case WindowMessage.WM_PAINT:
                    var hdc = BeginPaint(hWnd, out var ps);

                    if (ps.fErase)
                    {
                        //SelectObject(hdc, hBrush);
                        FillRect(hdc, ps.rcPaint, hBrush);
                    }
                    EndPaint(hWnd, ps);
                    return 0;
                case WindowMessage.WM_DESTROY:
                    SetMenu(hWnd, HMENU.NULL);
                    DestroyMenu(hMenu);

                    PostQuitMessage(0);
                    return 0;
                default:
                    return DefWindowProc(hWnd, msg, wParam, lParam);
            }


        }

        static nint ChildWndProc(HWND hWnd, uint msg, nint wParam, nint lParam)
        {
            switch ((WindowMessage)msg)
            {
                case WindowMessage.WM_CREATE:
                    SetWindowLong(hWnd, WindowLongFlags.DWLP_MSGRESULT, 0);

                    return 0;
                case WindowMessage.WM_LBUTTONDOWN:
                    SetWindowLong(hWnd, WindowLongFlags.DWLP_MSGRESULT, 1 ^ GetWindowLong(hWnd, WindowLongFlags.DWLP_MSGRESULT));
                    InvalidateRect(hWnd, null, false);
                    return 0;
                case WindowMessage.WM_PAINT:
                    var hdc = BeginPaint(hWnd, out var ps);

                        

                    GetClientRect(hWnd, out var rect);

                    SelectBrush(hdc, hBrush);

                    Rectangle(hdc, 0, 0, rect.right, rect.bottom);

                    if (GetWindowLong(hWnd, WindowLongFlags.DWLP_MSGRESULT) != 0)
                    {
                        MoveToEx(hdc, 0, 0, out _);
                        LineTo(hdc, rect.right, rect.bottom);
                        MoveToEx(hdc, 0, rect.bottom, out _);
                        LineTo(hdc, rect.right, 0);
                    }

                    EndPaint(hWnd, ps);
                    return 0;
                default:
                    return DefWindowProc(hWnd, msg, wParam, lParam);
            }


        }


        static SafeHMENU CreateAppMenu(HWND hWnd)
        {
            var hmenu = CreateMenu();

            var hmenupopup = CreatePopupMenu();

            AppendMenu(hmenupopup, MenuFlags.MF_STRING, IDM_FILE_QUIT, "&Quit");

            AppendMenu(hmenu, MenuFlags.MF_POPUP, hmenupopup.DangerousGetHandle(), "&File");

            hmenupopup = CreatePopupMenu();

            AppendMenu(hmenupopup, MenuFlags.MF_STRING, IDM_BACKGROUND_WHITE, "White");
            AppendMenu(hmenupopup, MenuFlags.MF_STRING, IDM_BACKGROUND_GRAY, "Gray");

            AppendMenu(hmenu, MenuFlags.MF_POPUP, hmenupopup.DangerousGetHandle(), "&Background");

            hmenupopup = CreatePopupMenu();

            AppendMenu(hmenupopup, MenuFlags.MF_STRING, IDM_HELP_ABOUT, "A&bout");

            AppendMenu(hmenu, MenuFlags.MF_POPUP, hmenupopup.DangerousGetHandle(), "&Help");

            return hmenu;
        }

        const nint IDM_FILE = 40_100;
        const nint IDM_BACKGROUND = 40_200;
        const nint IDM_HELP = 40_300;
        const nint IDM_FILE_QUIT = 40_101;
        const nint IDM_BACKGROUND_WHITE = 40_201;
        const nint IDM_BACKGROUND_GRAY = 40_202;
        const nint IDM_HELP_ABOUT = 40_301;
    }
}
