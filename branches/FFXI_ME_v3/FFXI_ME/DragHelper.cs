using System;
using System.Runtime.InteropServices;

namespace FFXI_ME_v2
{
    public class DragHelper
	{
		[DllImport("comctl32.dll")]
		public static extern bool InitCommonControls();

        /// <summary>
        /// Begins dragging an image.
        /// </summary>
        /// <param name="himlTrack">A handle to the image list.</param>
        /// <param name="iTrack">The index of the image to drag.</param>
        /// <param name="dxHotspot">The x-coordinate of the location of the drag position relative to the upper-left corner of the image.</param>
        /// <param name="dyHotspot">The y-coordinate of the location of the drag position relative to the upper-left corner of the image.</param>
        /// <returns>Returns true if successful, or false otherwise.</returns>
        /// <remarks>This function creates a temporary image list that is used for dragging. In response to subsequent WM_MOUSEMOVE messages, you can move the drag image by using the ImageList_DragMove function. To end the drag operation, you can use the ImageList_EndDrag function.</remarks>
		[DllImport("comctl32.dll", CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ImageList_BeginDrag(IntPtr himlTrack, int
			iTrack, int dxHotspot, int dyHotspot);

        /// <summary>
        /// Moves the image that is being dragged during a drag-and-drop operation. This function is typically called in response to a WM_MOUSEMOVE message.
        /// </summary>
        /// <param name="x">The x-coordinate at which to display the drag image. The coordinate is relative to the upper-left corner of the window, not the client area.</param>
        /// <param name="y">The y-coordinate at which to display the drag image. The coordinate is relative to the upper-left corner of the window, not the client area.</param>
        /// <returns>Returns true if successful, or false otherwise.</returns>
        /// <remarks>To begin a drag operation, use the ImageList_BeginDrag function.</remarks>
        [DllImport("comctl32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ImageList_DragMove(int x, int y);

        /// <summary>
        /// Ends a drag operation.
        /// </summary>
        /// <remarks>The temporary image list is destroyed when the ImageList_EndDrag function is called. To begin a drag operation, use the ImageList_BeginDrag function.</remarks>
		[DllImport("comctl32.dll", CharSet=CharSet.Auto)]
		public static extern void ImageList_EndDrag();

        /// <summary>
        /// Displays the drag image at the specified position within the window.
        /// </summary>
        /// <param name="hwndLock">A handle to the window that owns the drag image.</param>
        /// <param name="x">The x-coordinate at which to display the drag image. The coordinate is relative to the upper-left corner of the window, not the client area.</param>
        /// <param name="y">The y-coordinate at which to display the drag image. The coordinate is relative to the upper-left corner of the window, not the client area.</param>
        /// <returns>Returns true if successful, or false otherwise.</returns>
        /// <remarks>To begin a drag operation, use the ImageList_BeginDrag function.</remarks>
		[DllImport("comctl32.dll", CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ImageList_DragEnter(IntPtr hwndLock, int x, int y);

        /// <summary>
        /// Unlocks the specified window and hides the drag image, allowing the window to be updated.
        /// </summary>
        /// <param name="hwndLock">A handle to the window that owns the drag image.</param>
        /// <returns>Returns true if successful, or false otherwise.</returns>
		[DllImport("comctl32.dll", CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ImageList_DragLeave(IntPtr hwndLock);

        /// <summary>
        /// Shows or hides the image being dragged.
        /// </summary>
        /// <param name="fShow">A value specifying whether to show or hide the image being dragged. Specify TRUE to show the image or FALSE to hide the image.</param>
        /// <returns>Returns true if successful, or false otherwise.</returns>
		[DllImport("comctl32.dll", CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ImageList_DragShowNolock(bool fShow);

		static DragHelper()
		{
			InitCommonControls();
		}
	}
}
