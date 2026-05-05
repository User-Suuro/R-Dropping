Imports Guna.UI2.WinForms
Imports System.IO

Public Class BaseImagePanel
    Inherits PrimaryPanel
    Implements IValueProvider
    Implements IValidationStyleable

    Public Event ValueChanged As EventHandler Implements IValueProvider.ValueChanged

    Private _lblTitle As BaseLabel
    Private _lblPlaceholder As Label
    Private _pictureBox As PictureBox
    Private _btnRemove As Guna2Button

    ' Relative path from solution root, e.g. "images\items\item_20260505_143022_1234.jpg"
    ' Set immediately when the user picks an image — never Empty while an image is loaded.
    Private _relativePath As String = String.Empty
    Private _pendingSourceExtension As String = ".jpg"

    Private Const RemoveButtonSize As Integer = 24
    Private Const ImageFolder As String = "images"
    Private Const ImageSubFolder As String = "items"

    Private Shared ReadOnly AllowedExtensions As String() = {".jpg", ".jpeg", ".png"}

    ' ── Constructor ──────────────────────────────────────────────────────────

    Public Sub New()
        MyBase.New()
        Me.DoubleBuffered = True
        Me.Height = 160
        InitializeComponents()
        AttachEvents()
        UpdateState()
    End Sub

    ' ── UI Init ──────────────────────────────────────────────────────────────

    Private Sub InitializeComponents()
        _lblTitle = New BaseLabel()
        With _lblTitle
            .SetSmall()
            .Dock = DockStyle.Top
            .Padding = New Padding(0, 0, 0, 4)
        End With

        _pictureBox = New PictureBox()
        With _pictureBox
            .Dock = DockStyle.Fill
            .SizeMode = PictureBoxSizeMode.Zoom
            .BackColor = Color.FromArgb(245, 247, 250)
            .Cursor = Cursors.Hand
            .BorderStyle = BorderStyle.None
        End With

        _lblPlaceholder = New Label()
        With _lblPlaceholder
            .AutoSize = False
            .Dock = DockStyle.Fill
            .TextAlign = ContentAlignment.MiddleCenter
            .Text = "Click here to add Image"
            .ForeColor = Colors.LblMuted
            .Font = New Font("Segoe UI", 9.0F)
            .BackColor = Color.Transparent
            .Cursor = Cursors.Hand
        End With

        _btnRemove = New Guna2Button()
        With _btnRemove
            .Size = New Size(RemoveButtonSize, RemoveButtonSize)
            .BorderRadius = RemoveButtonSize \ 2
            .Text = "✕"
            .Font = New Font("Segoe UI", 7.5F, FontStyle.Bold)
            .FillColor = Color.FromArgb(220, 53, 69)
            .ForeColor = Color.White
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            .Cursor = Cursors.Hand
            .Visible = False
        End With

        Me.Controls.Add(_pictureBox)
        Me.Controls.Add(_lblTitle)
        _pictureBox.Controls.Add(_lblPlaceholder)
        _pictureBox.Controls.Add(_btnRemove)

        PositionRemoveButton()
    End Sub

    Private Sub AttachEvents()
        AddHandler _pictureBox.Click, AddressOf DropZone_Click
        AddHandler _lblPlaceholder.Click, AddressOf DropZone_Click

        AddHandler _btnRemove.Click,
            Sub(s As Object, e As EventArgs)
                ClearImage()
            End Sub

        AddHandler _pictureBox.Resize,
            Sub(s As Object, e As EventArgs)
                PositionRemoveButton()
            End Sub
    End Sub

    ' ── File Picking ─────────────────────────────────────────────────────────

    Private Sub DropZone_Click(sender As Object, e As EventArgs)
        Using dlg As New OpenFileDialog()
            dlg.Title = "Select an Image"
            dlg.Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
            dlg.FilterIndex = 1
            If dlg.ShowDialog() = DialogResult.OK Then
                TryLoadImage(dlg.FileName)
            End If
        End Using
    End Sub

    ''' <summary>
    ''' Loads an image from an absolute source path, computes the future
    ''' destination path immediately, and exposes it via Value.
    ''' </summary>
    Private Sub TryLoadImage(filePath As String)
        Dim ext As String = Path.GetExtension(filePath).ToLowerInvariant()

        ' ── FIX: use Array.IndexOf instead of .Contains() ────────────────
        If Array.IndexOf(AllowedExtensions, ext) = -1 Then
            OnValidationError()
            MessageBox.Show(
                "Only JPG and PNG images are supported." & Environment.NewLine &
                $"Selected file type: {ext}",
                "Invalid File Type",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
            Return
        End If

        Try
            Dim img As Image
            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read)
                img = Image.FromStream(fs)
            End Using

            Dim previous = _pictureBox.Image
            _pictureBox.Image = img
            previous?.Dispose()

            _pendingSourceExtension = ext

            ' ── Compute the future save path right now so Value is never "<pending>" ──
            _relativePath = BuildRelativePath(ext)

            OnValidationClear()
            UpdateState()
            RaiseEvent ValueChanged(Me, EventArgs.Empty)

        Catch ex As Exception
            OnValidationError()
            MessageBox.Show(
                "The selected file could not be loaded as an image." & Environment.NewLine & ex.Message,
                "Load Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        End Try
    End Sub

    ' ── Path Helpers ─────────────────────────────────────────────────────────

    ''' <summary>
    ''' Builds the relative path the image will be saved to:
    ''' images\items\item_yyyyMMdd_HHmmss_ffff.ext
    ''' </summary>
    Private Shared Function BuildRelativePath(ext As String) As String
        Dim timestamp As String = DateTime.Now.ToString("yyyyMMdd_HHmmss_ffff")
        Dim fileName As String = $"item_{timestamp}{ext}"
        Return Path.Combine(ImageFolder, ImageSubFolder, fileName)
    End Function

    ''' <summary>
    ''' Resolves any stored relative/absolute path to a full absolute path.
    ''' </summary>
    Private Function ResolveFullPath(pathOrName As String) As String
        If Path.IsPathRooted(pathOrName) Then Return pathOrName

        Dim slnRoot As String = FindSolutionRoot()
        If slnRoot Is Nothing Then Return Nothing

        ' Already starts with "images\..." — just combine with root.
        If pathOrName.StartsWith(ImageFolder & Path.DirectorySeparatorChar,
                                 StringComparison.OrdinalIgnoreCase) Then
            Return Path.Combine(slnRoot, pathOrName)
        End If

        ' Bare filename — assume it lives in the items subfolder.
        Return Path.Combine(slnRoot, ImageFolder, ImageSubFolder, pathOrName)
    End Function

    Private Shared Function FindSolutionRoot() As String
        Dim current As New DirectoryInfo(Application.StartupPath)
        Do While current IsNot Nothing
            If current.GetFiles("*.sln").Length > 0 Then Return current.FullName
            current = current.Parent
        Loop
        Return Nothing
    End Function

    ' ── Save ─────────────────────────────────────────────────────────────────

    ''' <summary>
    ''' Saves the image to the path that was already computed when the user
    ''' picked it (Value). Returns the relative path on success, Nothing on failure.
    ''' </summary>
    Public Function SaveImage() As String
        If _pictureBox.Image Is Nothing OrElse String.IsNullOrEmpty(_relativePath) Then
            Return Nothing
        End If

        Dim slnRoot As String = FindSolutionRoot()
        If slnRoot Is Nothing Then
            MessageBox.Show(
                "Could not locate the solution (.sln) folder." & Environment.NewLine &
                "Make sure you are running from within the project directory.",
                "Save Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
            Return Nothing
        End If

        Dim fullPath As String = Path.Combine(slnRoot, _relativePath)

        Try
            ' Ensure directory exists.
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath))

            Dim fmt As System.Drawing.Imaging.ImageFormat =
                If(_pendingSourceExtension = ".png",
                   System.Drawing.Imaging.ImageFormat.Png,
                   System.Drawing.Imaging.ImageFormat.Jpeg)

            _pictureBox.Image.Save(fullPath, fmt)

            ' Value is already correct — fire change so any listener re-reads it.
            RaiseEvent ValueChanged(Me, EventArgs.Empty)

            Return _relativePath

        Catch ex As Exception
            MessageBox.Show(
                "Failed to save the image." & Environment.NewLine & ex.Message,
                "Save Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
            Return Nothing
        End Try
    End Function

    ' ── Clear / Update ───────────────────────────────────────────────────────

    Public Sub ClearImage()
        Dim previous = _pictureBox.Image
        _pictureBox.Image = Nothing
        previous?.Dispose()
        _relativePath = String.Empty
        _pendingSourceExtension = ".jpg"
        OnValidationClear()
        UpdateState()
        RaiseEvent ValueChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub UpdateState()
        Dim hasImage As Boolean = (_pictureBox.Image IsNot Nothing)
        _lblPlaceholder.Visible = Not hasImage
        _btnRemove.Visible = hasImage
        If hasImage Then _pictureBox.SizeMode = PictureBoxSizeMode.Zoom
    End Sub

    Private Sub PositionRemoveButton()
        _btnRemove.Location = New Point(_pictureBox.Width - RemoveButtonSize - 6, 6)
    End Sub

    ' ── Load from existing path (edit mode) ──────────────────────────────────

    ''' <summary>
    ''' Loads an already-saved image by its stored relative path or absolute path.
    ''' Use this in edit mode to pre-populate the panel.
    ''' </summary>
    Public Sub LoadImage(pathOrName As String)
        If String.IsNullOrWhiteSpace(pathOrName) Then
            ClearImage()
            Return
        End If

        Dim fullPath As String = ResolveFullPath(pathOrName)

        If fullPath Is Nothing OrElse Not File.Exists(fullPath) Then
            OnValidationError()
            MessageBox.Show(
                $"Image file not found:{Environment.NewLine}{pathOrName}",
                "Load Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
            Return
        End If

        Dim ext As String = Path.GetExtension(fullPath).ToLowerInvariant()

        ' ── FIX: use Array.IndexOf ────────────────────────────────────────
        If Array.IndexOf(AllowedExtensions, ext) = -1 Then
            OnValidationError()
            MessageBox.Show(
                "Only JPG and PNG images are supported." & Environment.NewLine &
                $"File type detected: {ext}",
                "Invalid File Type",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
            Return
        End If

        Try
            Dim img As Image
            Using fs As New FileStream(fullPath, FileMode.Open, FileAccess.Read)
                img = Image.FromStream(fs)
            End Using

            Dim previous = _pictureBox.Image
            _pictureBox.Image = img
            previous?.Dispose()

            ' Keep the existing relative path as-is (already saved).
            _relativePath = Path.Combine(ImageFolder, ImageSubFolder,
                                                    Path.GetFileName(fullPath))
            _pendingSourceExtension = ext

            OnValidationClear()
            UpdateState()
            RaiseEvent ValueChanged(Me, EventArgs.Empty)

        Catch ex As Exception
            OnValidationError()
            MessageBox.Show(
                "The image could not be loaded." & Environment.NewLine & ex.Message,
                "Load Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Loads a saved image by bare filename (no extension).
    ''' Tries .jpg, .jpeg, .png in order.
    ''' </summary>
    Public Sub LoadImageByName(fileName As String)
        If String.IsNullOrWhiteSpace(fileName) Then
            ClearImage()
            Return
        End If

        ' If it already has an extension, delegate directly.
        If Not String.IsNullOrEmpty(Path.GetExtension(fileName)) Then
            LoadImage(fileName)
            Return
        End If

        Dim slnRoot As String = FindSolutionRoot()
        If slnRoot Is Nothing Then
            OnValidationError()
            MessageBox.Show("Could not locate the solution folder.", "Load Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim imagesDir As String = Path.Combine(slnRoot, ImageFolder, ImageSubFolder)

        For Each ext As String In AllowedExtensions
            Dim candidate As String = Path.Combine(imagesDir, fileName & ext)
            If File.Exists(candidate) Then
                LoadImage(candidate)
                Return
            End If
        Next

        OnValidationError()
        MessageBox.Show(
            $"No image named ""{fileName}"" (.jpg/.jpeg/.png) was found in the images\items folder.",
            "File Not Found",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning)
    End Sub

    ' ── Public Properties ────────────────────────────────────────────────────

    ''' <summary>
    ''' Returns the relative path where this image will be (or already is) saved.
    ''' e.g. "images\items\item_20260505_143022_1234.jpg"
    ''' Returns String.Empty when no image is loaded.
    ''' Satisfies both direct access and IValueProvider.
    ''' </summary>
    Public ReadOnly Property Value As String Implements IValueProvider.Value
        Get
            Return _relativePath   ' Empty when no image; real path the moment one is picked.
        End Get
    End Property

    Public ReadOnly Property ImageValue As Image
        Get
            Return _pictureBox.Image
        End Get
    End Property

    Public ReadOnly Property HasImage As Boolean
        Get
            Return _pictureBox.Image IsNot Nothing
        End Get
    End Property

    Public Property LabelText As String
        Get
            Return _lblTitle.Text
        End Get
        Set(value As String)
            _lblTitle.Text = value
        End Set
    End Property

    Public Property PlaceholderText As String
        Get
            Return _lblPlaceholder.Text
        End Get
        Set(value As String)
            _lblPlaceholder.Text = value
        End Set
    End Property

    ' ── Validation Styling ───────────────────────────────────────────────────

    Public Sub OnValidationError() Implements IValidationStyleable.OnValidationError
        _pictureBox.BackColor = Color.FromArgb(255, 235, 238)
    End Sub

    Public Sub OnValidationClear() Implements IValidationStyleable.OnValidationClear
        _pictureBox.BackColor = Color.FromArgb(245, 247, 250)
    End Sub

    ' ── Dispose ──────────────────────────────────────────────────────────────

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then _pictureBox.Image?.Dispose()
        MyBase.Dispose(disposing)
    End Sub

End Class