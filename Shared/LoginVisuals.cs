using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PosLoginUi;

internal enum LoginInputKind
{
    User,
    Password
}

internal static class LoginArtwork
{
    public static Bitmap RenderCover(Image sourceImage, Size targetSize)
    {
        var bitmap = new Bitmap(targetSize.Width, targetSize.Height, PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.FromArgb(234, 242, 255));
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;

        float scale = Math.Max(
            targetSize.Width / (float)sourceImage.Width,
            targetSize.Height / (float)sourceImage.Height);
        int width = (int)Math.Ceiling(sourceImage.Width * scale);
        int height = (int)Math.Ceiling(sourceImage.Height * scale);
        int x = (targetSize.Width - width) / 2;
        int y = (targetSize.Height - height) / 2;
        graphics.DrawImage(sourceImage, new Rectangle(x, y, width, height));
        return bitmap;
    }
}

internal sealed class LoginBackgroundPanel : Panel
{
    private Image? sourceImage;
    private Bitmap? renderedBackground;

    public LoginBackgroundPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WsClipChildren = 0x02000000;
            CreateParams parameters = base.CreateParams;
            parameters.Style |= WsClipChildren;
            return parameters;
        }
    }

    public void SetBackgroundImage(Image image)
    {
        sourceImage?.Dispose();
        sourceImage = image;
        RebuildBackground();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        RebuildBackground();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (renderedBackground is not null)
        {
            e.Graphics.DrawImageUnscaled(renderedBackground, Point.Empty);
            return;
        }

        e.Graphics.Clear(Color.FromArgb(234, 242, 255));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            renderedBackground?.Dispose();
            sourceImage?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void RebuildBackground()
    {
        renderedBackground?.Dispose();
        renderedBackground = null;

        if (sourceImage is null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            Invalidate();
            return;
        }

        var bitmap = new Bitmap(ClientSize.Width, ClientSize.Height, PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.FromArgb(234, 242, 255));
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;

        float scale = Math.Max(
            ClientSize.Width / (float)sourceImage.Width,
            ClientSize.Height / (float)sourceImage.Height);
        int width = (int)Math.Ceiling(sourceImage.Width * scale);
        int height = (int)Math.Ceiling(sourceImage.Height * scale);
        int x = (ClientSize.Width - width) / 2;
        int y = (ClientSize.Height - height) / 2;

        graphics.DrawImage(sourceImage, new Rectangle(x, y, width, height));
        renderedBackground = bitmap;
        Invalidate();
    }
}

internal sealed class LoginCardPanel : Panel
{
    private const int CardInset = 14;
    private const int CornerRadius = 28;

    public LoginCardPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WsClipChildren = 0x02000000;
            CreateParams parameters = base.CreateParams;
            parameters.Style |= WsClipChildren;
            return parameters;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var cardBounds = new Rectangle(
            CardInset,
            CardInset,
            Width - (CardInset * 2) - 1,
            Height - (CardInset * 2) - 1);

        for (int i = 8; i >= 1; i--)
        {
            var shadowBounds = cardBounds;
            shadowBounds.Inflate(i, i);
            shadowBounds.Offset(0, i / 2);
            using var shadowPath = CreateRoundedRectangle(shadowBounds, CornerRadius + i);
            using var shadowBrush = new SolidBrush(Color.FromArgb(5, 9, 35, 85));
            e.Graphics.FillPath(shadowBrush, shadowPath);
        }

        using var cardPath = CreateRoundedRectangle(cardBounds, CornerRadius);
        using var cardBrush = new SolidBrush(Color.FromArgb(252, 253, 255));
        using var borderPen = new Pen(Color.FromArgb(235, 240, 249));
        e.Graphics.FillPath(cardBrush, cardPath);
        e.Graphics.DrawPath(borderPen, cardPath);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class LoginAvatarControl : Control
{
    public LoginAvatarControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var circle = new Rectangle(2, 2, Width - 5, Height - 5);
        using var circleBrush = new LinearGradientBrush(
            circle,
            Color.FromArgb(238, 246, 255),
            Color.FromArgb(218, 233, 255),
            LinearGradientMode.Vertical);
        e.Graphics.FillEllipse(circleBrush, circle);

        using var personBrush = new SolidBrush(Color.FromArgb(10, 84, 218));
        int headSize = Math.Max(18, Width / 4);
        int headX = (Width - headSize) / 2;
        int headY = Height / 4;
        e.Graphics.FillEllipse(personBrush, headX, headY, headSize, headSize);

        var body = new Rectangle(
            Width / 4,
            headY + headSize + 5,
            Width / 2,
            Height / 3);
        using var bodyPath = new GraphicsPath();
        bodyPath.AddArc(body.Left, body.Top, body.Width, body.Height, 180, 180);
        bodyPath.AddLine(body.Right, body.Bottom, body.Left, body.Bottom);
        bodyPath.CloseFigure();
        e.Graphics.FillPath(personBrush, bodyPath);
    }
}

internal sealed class PasswordEyeButton : Control
{
    private bool mouseOver;

    public bool PasswordVisible { get; set; }

    public PasswordEyeButton()
    {
        Cursor = Cursors.Hand;
        TabStop = false;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.Selectable,
            true);
        BackColor = Color.Transparent;
        AccessibleName = "Tampilkan atau sembunyikan password";
        AccessibleRole = AccessibleRole.PushButton;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        mouseOver = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        mouseOver = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color color = mouseOver
            ? Color.FromArgb(10, 84, 218)
            : Color.FromArgb(112, 128, 155);

        var eyeBounds = new Rectangle(11, Height / 2 - 7, Width - 14, 14);
        using var eyePath = new GraphicsPath();
        eyePath.AddBezier(
            eyeBounds.Left, eyeBounds.Top + eyeBounds.Height / 2,
            eyeBounds.Left + eyeBounds.Width / 3, eyeBounds.Top - 2,
            eyeBounds.Right - eyeBounds.Width / 3, eyeBounds.Top - 2,
            eyeBounds.Right, eyeBounds.Top + eyeBounds.Height / 2);
        eyePath.AddBezier(
            eyeBounds.Right, eyeBounds.Top + eyeBounds.Height / 2,
            eyeBounds.Right - eyeBounds.Width / 3, eyeBounds.Bottom + 2,
            eyeBounds.Left + eyeBounds.Width / 3, eyeBounds.Bottom + 2,
            eyeBounds.Left, eyeBounds.Top + eyeBounds.Height / 2);
        eyePath.CloseFigure();

        using var pen = new Pen(color, 2F);
        e.Graphics.DrawPath(pen, eyePath);
        using var pupilBrush = new SolidBrush(color);
        e.Graphics.FillEllipse(
            pupilBrush,
            Width / 2 + 2,
            Height / 2 - 3,
            7,
            7);

        if (!PasswordVisible)
        {
            using var slashPen = new Pen(Color.FromArgb(252, 253, 255), 3F);
            e.Graphics.DrawLine(slashPen, 12, 7, Width - 3, Height - 7);
            using var outlinePen = new Pen(color, 1.5F);
            e.Graphics.DrawLine(outlinePen, 12, 6, Width - 3, Height - 6);
        }
    }
}

internal sealed class RoundedInputPanel : Panel
{
    private bool containsFocus;
    private LoginInputKind inputKind;

    public LoginInputKind InputKind
    {
        get => inputKind;
        set
        {
            inputKind = value;
            Padding = value == LoginInputKind.Password
                ? new Padding(48, 2, 50, 2)
                : new Padding(48, 2, 18, 2);
            Invalidate();
        }
    }

    public RoundedInputPanel()
    {
        Padding = new Padding(48, 2, 18, 2);
        BackColor = Color.Transparent;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        e.Control.Enter += ChildFocusChanged;
        e.Control.Leave += ChildFocusChanged;
    }

    protected override void OnControlRemoved(ControlEventArgs e)
    {
        e.Control.Enter -= ChildFocusChanged;
        e.Control.Leave -= ChildFocusChanged;
        base.OnControlRemoved(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new Rectangle(1, 1, Width - 3, Height - 3);
        using var path = CreateRoundedRectangle(bounds, 11);
        using var fill = new SolidBrush(Color.FromArgb(253, 254, 255));
        using var border = new Pen(
            containsFocus ? Color.FromArgb(15, 91, 220) : Color.FromArgb(199, 211, 230),
            containsFocus ? 2F : 1.25F);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);

        Color iconColor = containsFocus
            ? Color.FromArgb(15, 91, 220)
            : Color.FromArgb(116, 132, 160);
        DrawIcon(e.Graphics, new Rectangle(17, (Height - 22) / 2, 22, 22), iconColor);
    }

    private void ChildFocusChanged(object? sender, EventArgs e)
    {
        BeginInvoke(() =>
        {
            containsFocus = ContainsFocus;
            Invalidate();
        });
    }

    private void DrawIcon(Graphics graphics, Rectangle bounds, Color color)
    {
        using var pen = new Pen(color, 2F);
        using var brush = new SolidBrush(color);

        if (InputKind == LoginInputKind.User)
        {
            graphics.FillEllipse(
                brush,
                bounds.Left + 7,
                bounds.Top + 1,
                9,
                9);
            using var body = new GraphicsPath();
            body.AddArc(bounds.Left + 3, bounds.Top + 11, 17, 14, 180, 180);
            body.AddLine(bounds.Right - 2, bounds.Bottom, bounds.Left + 2, bounds.Bottom);
            body.CloseFigure();
            graphics.FillPath(brush, body);
            return;
        }

        var lockBody = new Rectangle(bounds.Left + 4, bounds.Top + 9, 15, 12);
        graphics.DrawArc(pen, bounds.Left + 7, bounds.Top + 1, 9, 14, 180, -180);
        graphics.FillRectangle(brush, lockBody);
        using var keyBrush = new SolidBrush(Color.FromArgb(253, 254, 255));
        graphics.FillEllipse(keyBrush, bounds.Left + 10, bounds.Top + 12, 4, 4);
        graphics.FillRectangle(keyBrush, bounds.Left + 11, bounds.Top + 15, 2, 4);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class SecurityShieldControl : Control
{
    public SecurityShieldControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var points = new[]
        {
            new Point(Width / 2, 3),
            new Point(Width - 5, 9),
            new Point(Width - 7, Height - 11),
            new Point(Width / 2, Height - 3),
            new Point(7, Height - 11),
            new Point(5, 9)
        };
        using var pen = new Pen(Color.FromArgb(125, 145, 176), 2.5F);
        e.Graphics.DrawPolygon(pen, points);
        e.Graphics.DrawLines(
            pen,
            new[]
            {
                new Point(Width / 3, Height / 2),
                new Point(Width / 2 - 1, Height * 2 / 3),
                new Point(Width * 3 / 4, Height / 3)
            });
    }
}

internal sealed class RoundedActionButton : Button
{
    private bool mouseOver;
    private bool mouseDown;

    public int CornerRadius { get; set; } = 10;

    public RoundedActionButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = CreateRoundedRectangle(bounds, CornerRadius);

        Color fillColor = Enabled
            ? mouseDown
                ? Color.FromArgb(5, 62, 171)
                : mouseOver
                    ? Color.FromArgb(8, 74, 198)
                    : BackColor
            : Color.FromArgb(146, 174, 220);
        using var brush = new SolidBrush(fillColor);
        e.Graphics.FillPath(brush, path);
        if (Focused)
        {
            var focusBounds = bounds;
            focusBounds.Inflate(-4, -4);
            using var focusPath = CreateRoundedRectangle(focusBounds, Math.Max(4, CornerRadius - 4));
            using var focusPen = new Pen(Color.FromArgb(170, 220, 240, 255), 1F);
            e.Graphics.DrawPath(focusPen, focusPath);
        }
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            bounds,
            ForeColor,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        mouseOver = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        mouseOver = false;
        mouseDown = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        mouseDown = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        mouseDown = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class CircularCloseButton : Button
{
    private bool mouseOver;

    public CircularCloseButton()
    {
        Cursor = Cursors.Hand;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        TabStop = false;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(2, 2, Width - 5, Height - 5);
        using var fill = new SolidBrush(
            mouseOver
                ? Color.FromArgb(236, 244, 255)
                : Color.FromArgb(218, 231, 249));
        using var border = new Pen(Color.FromArgb(188, 209, 238));
        e.Graphics.FillEllipse(fill, bounds);
        e.Graphics.DrawEllipse(border, bounds);

        Color xColor = mouseOver
            ? Color.FromArgb(8, 68, 174)
            : Color.FromArgb(29, 78, 145);
        using var pen = new Pen(xColor, 2F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        int inset = Width / 3;
        e.Graphics.DrawLine(pen, inset, inset, Width - inset, Height - inset);
        e.Graphics.DrawLine(pen, Width - inset, inset, inset, Height - inset);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        mouseOver = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        mouseOver = false;
        Invalidate();
        base.OnMouseLeave(e);
    }
}
