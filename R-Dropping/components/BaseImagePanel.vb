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

    ' Stores only the relative path from the solution root (e.g. "images\product_42.jpg")
    Private _relativePath As String = String.Empty

    Private Const PanelHeight As Integer = 160
    Private Const RemoveButtonSize As Integer = 24
    Private Const ImageFolder As String = "images"
    Private ReadOnly AllowedExtensions As String() = {".jpg", ".jpeg", ".png"}

    Public Sub New()
        MyBase.New()
        Me.DoubleBuffered = True
        Me.Height = PanelHeight
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

    Private Sub TryLoadImage(filePath As String)
        Dim ext As String = Path.GetExtension(filePath).ToLowerInvariant()

        If Not AllowedExtensions.Contains(ext) Then
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

            ' Keep the original extension so SaveImage knows what format to use.
            ' _relativePath is empty until SaveImage is called.
            _relativePath = String.Empty
            _pendingSourceExtension = ext   ' remembered for SaveImage

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

    ' Holds the file extension of the most recently picked image until SaveImage is called.
    Private _pendingSourceExtension As String = ".jpg"

    ' ── Save ─────────────────────────────────────────────────────────────────

    ''' <summary>
    ''' Saves the current image to  &lt;sln-root&gt;\images\{itemId}.{ext}
    ''' and updates <see cref="Value"/> to the relative path  images\{itemId}.{ext}.
    ''' Returns the relative path on success, or Nothing if there is no image to save.
    ''' </summary>
    ''' <param name="itemId">
    ''' The identifier used as the filename, e.g. "product_42" → images\product_42.jpg
    ''' </param>
    Public Function SaveImage(itemId As String) As String
        If _pictureBox.Image Is Nothing Then Return Nothing

        ' 1. Locate the solution root (walks up from bin\Debug or bin\Release).
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

        ' 2. Ensure the images sub-folder exists.
        Dim imagesDir As String = Path.Combine(slnRoot, ImageFolder)
        Directory.CreateDirectory(imagesDir)

        ' 3. Build the full path and the relative path.
        Dim ext As String = _pendingSourceExtension   ' e.g. ".jpg"
        Dim fileName As String = itemId & ext          ' e.g. "product_42.jpg"
        Dim fullPath As String = Path.Combine(imagesDir, fileName)
        Dim relativePath As String = Path.Combine(ImageFolder, fileName)  ' "images\product_42.jpg"

        ' 4. Write the file in the correct format.
        Try
            Dim fmt As System.Drawing.Imaging.ImageFormat =
                If(ext = ".png",
                   System.Drawing.Imaging.ImageFormat.Png,
                   System.Drawing.Imaging.ImageFormat.Jpeg)

            _pictureBox.Image.Save(fullPath, fmt)

            ' 5. Switch Value to the relative path now that the file exists.
            _relativePath = relativePath
            RaiseEvent ValueChanged(Me, EventArgs.Empty)

            Return relativePath

        Catch ex As Exception
            MessageBox.Show(
                "Failed to save the image." & Environment.NewLine & ex.Message,
                "Save Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
            Return Nothing
        End Try
    End Function

    ' ── SLN Root Resolver ────────────────────────────────────────────────────

    ''' <summary>
    ''' Walks up the directory tree from the application's startup path
    ''' (typically bin\Debug or bin\Release) until it finds a folder that
    ''' contains at least one .sln file, then returns that folder path.
    ''' Returns Nothing if no .sln is found before reaching the drive root.
    ''' </summary>
    Private Shared Function FindSolutionRoot() As String
        Dim current As New DirectoryInfo(Application.StartupPath)

        Do While current IsNot Nothing
            If current.GetFiles("*.sln").Length > 0 Then
                Return current.FullName
            End If
            current = current.Parent
        Loop

        Return Nothing
    End Function

    ' ── State Management ─────────────────────────────────────────────────────

    Private Sub UpdateState()
        Dim hasImage As Boolean = (_pictureBox.Image IsNot Nothing)
        _lblPlaceholder.Visible = Not hasImage
        _btnRemove.Visible = hasImage
        If hasImage Then _pictureBox.SizeMode = PictureBoxSizeMode.Zoom
    End Sub

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

    Private Sub PositionRemoveButton()
        _btnRemove.Location = New Point(_pictureBox.Width - RemoveButtonSize - 6, 6)
    End Sub

    ' ── Load / Set ───────────────────────────────────────────────────────────────

    ''' <summary>
    ''' Loads an image from either a relative path (e.g. "images\product_42.jpg"
    ''' or just "product_42.jpg") or a full absolute path.
    ''' The stored <see cref="Value"/> is normalised to the relative form
    ''' "images\{filename}" so it stays portable across machines.
    ''' </summary>
    ''' <param name="pathOrName">
    ''' Accepts any of the following:
    '''   • Relative path  → "images\product_42.jpg"
    '''   • Filename only  → "product_42.jpg"  (looked up in the images folder)
    '''   • Absolute path  → "C:\Projects\MyApp\images\product_42.jpg"
    ''' </param>
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

        If Not AllowedExtensions.Contains(ext) Then
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

            ' Always store in normalised relative form: "images\product_42.jpg"
            _relativePath = Path.Combine(ImageFolder, Path.GetFileName(fullPath))
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
    ''' Shorthand — accepts just the filename stem with or without extension,
    ''' e.g. "product_42" or "product_42.jpg".  Tries .jpg then .png when
    ''' no extension is supplied.
    ''' </summary>
    Public Sub LoadImageByName(fileName As String)
        If String.IsNullOrWhiteSpace(fileName) Then
            ClearImage()
            Return
        End If

        ' If an extension is already present, delegate directly.
        If Not String.IsNullOrEmpty(Path.GetExtension(fileName)) Then
            LoadImage(fileName)
            Return
        End If

        ' No extension supplied — probe supported formats in priority order.
        Dim slnRoot As String = FindSolutionRoot()
        If slnRoot Is Nothing Then
            OnValidationError()
            MessageBox.Show(
            "Could not locate the solution folder.",
            "Load Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning)
            Return
        End If

        Dim imagesDir As String = Path.Combine(slnRoot, ImageFolder)

        For Each ext As String In AllowedExtensions
            Dim candidate As String = Path.Combine(imagesDir, fileName & ext)
            If File.Exists(candidate) Then
                LoadImage(candidate)
                Return
            End If
        Next

        ' Nothing matched.
        OnValidationError()
        MessageBox.Show(
        $"No image named ""{fileName}"" (.jpg / .jpeg / .png) was found in the images folder.",
        "File Not Found",
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning)
    End Sub

    ' ── Path Resolver (private) ──────────────────────────────────────────────────

    ''' <summary>
    ''' Turns any of the three accepted input forms into a verified absolute path,
    ''' or returns Nothing if the root cannot be determined.
    '''
    '''   Absolute path  →  returned as-is
    '''   "images\x.jpg" →  &lt;slnRoot&gt;\images\x.jpg
    '''   "x.jpg"        →  &lt;slnRoot&gt;\images\x.jpg
    ''' </summary>
    Private Function ResolveFullPath(pathOrName As String) As String
        ' Already absolute — trust it directly.
        If Path.IsPathRooted(pathOrName) Then
            Return pathOrName
        End If

        Dim slnRoot As String = FindSolutionRoot()
        If slnRoot Is Nothing Then Return Nothing

        ' Relative path already starts with the images folder prefix.
        If pathOrName.StartsWith(ImageFolder & Path.DirectorySeparatorChar,
                              StringComparison.OrdinalIgnoreCase) Then
            Return Path.Combine(slnRoot, pathOrName)
        End If

        ' Bare filename — prepend the images folder automatically.
        Return Path.Combine(slnRoot, ImageFolder, pathOrName)
    End Function


    ' ── IValueProvider ───────────────────────────────────────────────────────

    ''' <summary>
    ''' Returns the relative image path (e.g. "images\product_42.jpg") after
    ''' <see cref="SaveImage"/> has been called, or an empty string otherwise.
    ''' </summary>
    Public ReadOnly Property Value As String Implements IValueProvider.Value
        Get
            Return _relativePath
        End Get
    End Property

    ''' <summary>Returns the in-memory Image object, or Nothing.</summary>
    Public ReadOnly Property ImageValue As Image
        Get
            Return _pictureBox.Image
        End Get
    End Property

    ' ── IValidationStyleable ─────────────────────────────────────────────────

    Public Sub OnValidationError() Implements IValidationStyleable.OnValidationError
        _pictureBox.BackColor = Color.FromArgb(255, 235, 238)
    End Sub

    Public Sub OnValidationClear() Implements IValidationStyleable.OnValidationClear
        _pictureBox.BackColor = Color.FromArgb(245, 247, 250)
    End Sub

    ' ── Public Properties ────────────────────────────────────────────────────

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

    Public ReadOnly Property HasImage As Boolean
        Get
            Return _pictureBox.Image IsNot Nothing
        End Get
    End Property

    ' ── Cleanup ──────────────────────────────────────────────────────────────

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then _pictureBox.Image?.Dispose()
        MyBase.Dispose(disposing)
    End Sub

End Class