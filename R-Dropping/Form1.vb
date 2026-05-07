Public Class Form1
    Public nav As NavigationManager

    Public Shared Instance As Form1
    Public mainPanel As New PrimaryPanel()

    ' --- DB Config fields ---
    Private configContainerPanel As PrimaryPanel
    Private configSubPanel As FlowLayoutPanel
    Private serverInput As BaseInputPanel
    Private uidInput As BaseInputPanel
    Private pwdInput As BaseInputPanel
    Private dbNameInput As BaseInputPanel
    Private dbPortInput As BaseInputPanel
    Private serverField As ValidationPanel
    Private uidField As ValidationPanel
    Private dbNameField As ValidationPanel
    Private dbPortField As ValidationPanel
    Private btnSubmit As BaseButton
    Private configVal As New DbConfig()

    ' --- Login fields ---
    Private loginContainerPanel As PrimaryPanel
    Private loginSubPanel As FlowLayoutPanel
    Private emailInput As BaseInputPanel
    Private passwordInput As BaseInputPanel
    Private emailField As ValidationPanel
    Private passwordField As ValidationPanel
    Private btnLogin As BaseButton

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Instance = Me
        mainPanel.Dock = DockStyle.Fill
        nav = New NavigationManager(mainPanel)

        With Me
            .WindowState = FormWindowState.Maximized
            .StartPosition = FormStartPosition.CenterScreen
            .Icon = My.Resources.Resource1.logo
            .MinimumSize = Dimen.MIN_RES
            .Text = Strings.APP_NAME
            .Controls.Add(mainPanel)
            .Padding = Padding.Empty
        End With

        Themes.ApplyLightTheme()
        Config.EnsureConfigExists(Of DbConfig)()
        ShowDbConfig()
    End Sub

    ' ===================== DB CONFIG SCREEN =====================

    Public Sub ShowDbConfig()
        mainPanel.Controls.Clear()

        Dim bg As New PictureBox()
        With bg
            .Dock = DockStyle.Fill
            .SizeMode = PictureBoxSizeMode.StretchImage
            .Image = My.Resources.Resource1.login_animation
        End With
        mainPanel.Controls.Add(bg)

        Dim overlay As Panel = OverlayPanel.CreateOverlay()
        overlay.Dock = DockStyle.Fill
        bg.Controls.Add(overlay)

        configContainerPanel = New PrimaryPanel()
        configSubPanel = New FlowLayoutPanel()
        serverInput = New BaseInputPanel()
        uidInput = New BaseInputPanel()
        pwdInput = New BaseInputPanel()
        dbNameInput = New BaseInputPanel()
        dbPortInput = New BaseInputPanel()
        btnSubmit = New BaseButton()

        With configContainerPanel
            .AutoSize = True
            .AutoSizeMode = AutoSizeMode.GrowAndShrink
        End With
        overlay.Controls.Add(configContainerPanel)

        With configSubPanel
            .Padding = New Padding(16)
            .FlowDirection = FlowDirection.TopDown
            .AutoSize = True
            .AutoSizeMode = AutoSizeMode.GrowAndShrink
            .BackColor = Color.White
        End With
        configContainerPanel.Controls.Add(configSubPanel)

        RenderConfigContent()
        LayoutHelper.CenterBoth(configContainerPanel)
        LayoutHelper.EnableAutoCenter(configContainerPanel)
    End Sub

    Private Sub RenderConfigContent()
        configVal = Config.Load(Of DbConfig)()

        With serverInput
            .LabelText = Strings.SERVER_LBL
            .InputControl.PlaceholderText = Strings.SERVER_PLACEHOLDER
            .InputControl.Text = configVal.DB_SERVER
        End With
        serverField = New ValidationPanel(serverInput)
        serverField.SetValidator(New InputValidator().Required())

        With uidInput
            .LabelText = Strings.UID_LBL
            .InputControl.PlaceholderText = Strings.UID_PLACEHOLDER
            .InputControl.Text = configVal.DB_UID
        End With
        uidField = New ValidationPanel(uidInput)
        uidField.SetValidator(New InputValidator().Required())

        With pwdInput
            .LabelText = Strings.DB_PASS_LBL
            .InputControl.PlaceholderText = Strings.DB_PASS_PLACEHOLDER
            .InputControl.UseSystemPasswordChar = True
            .InputControl.Text = configVal.DB_PWD
        End With

        With dbNameInput
            .LabelText = Strings.DB_NAME
            .InputControl.PlaceholderText = Strings.DB_NAME_PLACEHOLDER
            .InputControl.Text = configVal.DB_NAME
        End With
        dbNameField = New ValidationPanel(dbNameInput)
        dbNameField.SetValidator(New InputValidator().Required())

        With dbPortInput
            .LabelText = Strings.DB_PORT
            .InputControl.PlaceholderText = Strings.DB_PORT_PLACEHOLDER
            .InputControl.Text = configVal.DB_PORT
        End With
        dbPortField = New ValidationPanel(dbPortInput)
        dbPortField.SetValidator(New InputValidator().Required())

        With btnSubmit
            .Text = Strings.BTN_CONNECT
            .SetPrimary()
            .Margin = New Padding(0, 8, 0, 0)
        End With

        With configSubPanel.Controls
            .Add(serverField)
            .Add(dbPortField)
            .Add(uidField)
            .Add(pwdInput)
            .Add(dbNameField)
            .Add(btnSubmit)
        End With

        AddHandler btnSubmit.Click, AddressOf SaveConfig
    End Sub

    Private Async Sub SaveConfig()
        If Not ValidateDbInputs() Then Exit Sub

        Dim dlg As New BaseDialog()

        Try
            With configVal
                .DB_PORT = dbPortInput.InputControl.Text
                .DB_UID = uidInput.InputControl.Text
                .DB_SERVER = serverInput.InputControl.Text
                .DB_PWD = pwdInput.InputControl.Text
                .DB_NAME = dbNameInput.InputControl.Text
            End With

            Db.UpdateConnectionString(configVal.DB_SERVER,
                                      configVal.DB_PORT,
                                      configVal.DB_UID,
                                      configVal.DB_PWD,
                                      configVal.DB_NAME)
            Config.Save(configVal)

            Dim loadingDlg As New BaseDialog()

            Await DialogTypes.ShowLoadingUntilAsync(
                loadingDlg,
                Me,
                Async Function()
                    Dim connected As Boolean = Await IsConnectedAsync()

                    If Not connected Then
                        DialogTypes.Apply(dlg,
                            DialogType.Error,
                            "Failed to Connect",
                            "Database could not be found.")
                        dlg.ShowBaseDialog(Me)
                        Return
                    End If

                    ' ✅ Check if an admin account exists before going to login
                    Dim adminExists As Boolean = Await CheckAdminExists()

                    If adminExists Then
                        ShowLoginScreen()
                    Else
                        ShowInitialAdminSetup()
                    End If
                End Function
            )

        Catch ex As Exception
            DialogTypes.Apply(dlg,
                DialogType.Error,
                "Error Saving Configuration",
                "An error occurred while saving the configuration. Please try again.")
            dlg.ShowBaseDialog(Me)
        End Try
    End Sub

    Private Async Function CheckAdminExists() As Task(Of Boolean)
        Dim sql As String =
            $"SELECT COUNT(*) FROM {Employee.table_name}
              WHERE {Employee.position} = @position"

        Dim params As New Dictionary(Of String, Object) From {
            {"@position", "Admin"}
        }

        Try
            Using reader = Await ReadQueryAsync(sql, params)
                If reader IsNot Nothing AndAlso Await reader.ReadAsync() Then
                    Return Convert.ToInt32(reader(0)) > 0
                End If
            End Using
        Catch ex As Exception
            ' If query fails, fall through to login to be safe
        End Try

        Return False
    End Function

    Private Sub ShowInitialAdminSetup()
        nav.GoToPage(New EmployeeForm(
            lockPosition:="Admin",
            hideCancel:=True,
            onComplete:=AddressOf ShowLoginScreen
        ))
    End Sub

    Private Function ValidateDbInputs() As Boolean
        Return {serverField, uidField, dbNameField, dbPortField}.All(Function(f) f.ValidateInput())
    End Function

    ' ===================== LOGIN SCREEN =====================

    Public Sub ShowLoginScreen()
        mainPanel.Controls.Clear()

        Dim bg As New PictureBox()
        With bg
            .Dock = DockStyle.Fill
            .SizeMode = PictureBoxSizeMode.StretchImage
            .Image = My.Resources.Resource1.login_animation
        End With
        mainPanel.Controls.Add(bg)

        Dim overlay As Panel = OverlayPanel.CreateOverlay()
        overlay.Dock = DockStyle.Fill
        bg.Controls.Add(overlay)

        loginContainerPanel = New PrimaryPanel()
        loginSubPanel = New FlowLayoutPanel()
        emailInput = New BaseInputPanel()
        passwordInput = New BaseInputPanel()
        btnLogin = New BaseButton()

        With loginContainerPanel
            .AutoSize = True
            .AutoSizeMode = AutoSizeMode.GrowAndShrink
        End With
        overlay.Controls.Add(loginContainerPanel)

        With loginSubPanel
            .Padding = New Padding(16)
            .FlowDirection = FlowDirection.TopDown
            .AutoSize = True
            .AutoSizeMode = AutoSizeMode.GrowAndShrink
            .BackColor = Color.White
        End With
        loginContainerPanel.Controls.Add(loginSubPanel)

        RenderLoginContent()
        LayoutHelper.CenterBoth(loginContainerPanel)
        LayoutHelper.EnableAutoCenter(loginContainerPanel)
    End Sub

    Private Sub RenderLoginContent()
        With emailInput
            .LabelText = "Email"
            .InputControl.PlaceholderText = "Enter your email"
        End With
        emailField = New ValidationPanel(emailInput)
        emailField.SetValidator(New InputValidator().Required())

        With passwordInput
            .LabelText = "Password"
            .InputControl.PlaceholderText = "Enter your password"
            .InputControl.UseSystemPasswordChar = True
        End With
        passwordField = New ValidationPanel(passwordInput)
        passwordField.SetValidator(New InputValidator().Required())

        With btnLogin
            .Text = "Login"
            .SetPrimary()
            .Margin = New Padding(0, 8, 0, 0)
        End With

        With loginSubPanel.Controls
            .Add(emailField)
            .Add(passwordField)
            .Add(btnLogin)
        End With

        AddHandler btnLogin.Click, AddressOf DoLogin
    End Sub

    Private Async Sub DoLogin()
        If Not {emailField, passwordField}.All(Function(f) f.ValidateInput()) Then Exit Sub

        Dim dlg As New BaseDialog()
        Dim loadingDlg As New BaseDialog()

        Try
            Await DialogTypes.ShowLoadingUntilAsync(
                loadingDlg,
                Me,
                Async Function()
                    Await QueryLogin(dlg)
                End Function
            )

        Catch ex As Exception
            DialogTypes.Apply(dlg,
                DialogType.Error,
                "Login Error",
                "An unexpected error occurred. Please try again.")
            dlg.ShowBaseDialog(Me)
        End Try
    End Sub

    Private Async Function QueryLogin(dlg As BaseDialog) As Task
        Dim sql As String =
            $"SELECT {Employee.id}, {Employee.email}, {Employee.password}, {Employee.position}
              FROM {Employee.table_name}
              WHERE {Employee.email} = @email"

        Dim params As New Dictionary(Of String, Object) From {
            {"@email", emailInput.InputControl.Text.Trim()}
        }

        Try
            Using reader = Await ReadQueryAsync(sql, params)
                If reader Is Nothing OrElse Not Await reader.ReadAsync() Then
                    DialogTypes.Apply(dlg,
                        DialogType.Error,
                        "Login Failed",
                        "No account found with that email.")
                    dlg.ShowBaseDialog(Me)
                    Return
                End If

                Dim storedEncrypted As String = reader(Employee.password).ToString()
                Dim decrypted As String = session.Decrypt(storedEncrypted)

                If decrypted <> passwordInput.InputControl.Text Then
                    DialogTypes.Apply(dlg,
                        DialogType.Error,
                        "Login Failed",
                        "Incorrect password.")
                    dlg.ShowBaseDialog(Me)
                    Return
                End If

                Dim success As Boolean = session.Login(
                    CInt(reader(Employee.id)),
                    reader(Employee.email).ToString(),
                    passwordInput.InputControl.Text,
                    reader(Employee.position).ToString()
                )

                If success Then
                    nav.GoToPage(New root())
                Else
                    DialogTypes.Apply(dlg,
                        DialogType.Error,
                        "Login Failed",
                        "An error occurred during login.")
                    dlg.ShowBaseDialog(Me)
                End If
            End Using

        Catch ex As Exception
            DialogTypes.Apply(dlg,
                DialogType.Error,
                "Login Error",
                ex.Message)
            dlg.ShowBaseDialog(Me)
        End Try
    End Function

End Class

'  DIALOG COMPONENT 

Public Class BaseDialog
    Inherits Form

    Private ownerForm As Form

    Public Sub New()
        FormBorderStyle = FormBorderStyle.None
        ShowInTaskbar = False
        BackColor = Color.White
        StartPosition = FormStartPosition.CenterScreen
        BringToFront()
        AutoSize = True
        AutoSizeMode = AutoSizeMode.GrowAndShrink
        InitializeDialogUI()
    End Sub

    ' CONTENT 

    Private lblTitle As BaseLabel
    Private lblDescription As BaseLabel
    Private picIcon As PictureBox
    Private btnConfirm As BaseButton
    Private btnCancel As BaseButton
    Private buttonTable As TableLayoutPanel
    Private subContainer As PrimaryFlowLayoutPanel

    Private overlay As Panel
    Private ownerOverlay As DoubleBufferedPanel

    Public Property Result As DialogResultType = DialogResultType.None
    Public Event DialogClosed(result As DialogResultType)


    Private Sub InitializeDialogUI()


        subContainer = New PrimaryFlowLayoutPanel()

        With subContainer
            .Padding = New Padding(16)
            .FlowDirection = FlowDirection.TopDown
            .AutoSize = True
            .AutoSizeMode = AutoSizeMode.GrowAndShrink
            .Margin = Padding.Empty
            .BackColor = Color.White
            .BorderStyle = BorderStyle.FixedSingle
        End With

        Dim border As New Panel()



        ' TITLE
        lblTitle = New BaseLabel()
        With lblTitle
            .SetSmall()
            .Anchor = AnchorStyles.None
            .TextAlign = ContentAlignment.MiddleCenter
        End With

        ' ICON
        picIcon = New PictureBox With {
            .SizeMode = PictureBoxSizeMode.CenterImage,
            .Dock = DockStyle.Top,
            .BackColor = Color.White
        }

        ' DESCRIPTION
        lblDescription = New BaseLabel()
        With lblDescription
            .SetSmall()
            .TextAlign = ContentAlignment.MiddleCenter
            .Anchor = AnchorStyles.None
        End With

        ' BUTTON TABLE (2 columns)
        buttonTable = New TableLayoutPanel With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .ColumnCount = 2,
            .Padding = Padding.Empty,
            .Dock = DockStyle.Top,
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .BackColor = Color.White
        }

        buttonTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        buttonTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))

        ' BUTTONS
        btnConfirm = New BaseButton()
        With btnConfirm
            .Text = Strings.BTN_CONFIRM
            .Dock = DockStyle.Fill
            .SetPrimary()
            .Padding = Padding.Empty
            .Margin = Padding.Empty
        End With

        btnCancel = New BaseButton()
        With btnCancel
            .Text = Strings.BTN_CANCEL
            .Dock = DockStyle.Fill
            .SetDanger()
            .Padding = Padding.Empty
            .Margin = Padding.Empty
        End With

        AddHandler btnConfirm.Click, Sub()
                                         Result = DialogResultType.Confirm
                                         RaiseEvent DialogClosed(Result)
                                         Me.Close()
                                     End Sub

        AddHandler btnCancel.Click, Sub()
                                        Result = DialogResultType.Cancel
                                        RaiseEvent DialogClosed(Result)
                                        Me.Close()
                                    End Sub

        buttonTable.Controls.Add(btnCancel, 0, 0)
        buttonTable.Controls.Add(btnConfirm, 1, 0)

        Me.Controls.Add(subContainer)


        With subContainer.Controls
            .Add(lblTitle)
            .Add(picIcon)
            .Add(lblDescription)
            .Add(buttonTable)
        End With
    End Sub

    Public Sub SetTitle(text As String)
        lblTitle.Text = text
    End Sub

    Public Sub SetMessage(text As String)
        lblDescription.Text = text
    End Sub

    Public Sub SetIcon(img As Image)
        picIcon.Image = img
    End Sub

    Public Sub SetConfirmVisible(visible As Boolean)
        btnConfirm.Visible = visible
        UpdateButtonLayout()
    End Sub

    Public Sub SetCancelVisible(visible As Boolean)
        btnCancel.Visible = visible
        UpdateButtonLayout()
    End Sub

    Public Sub SetConfirmText(text As String)
        btnConfirm.Text = text
    End Sub

    Public Sub SetCancelText(text As String)
        btnCancel.Text = text
    End Sub

    Private Sub UpdateButtonLayout()
        buttonTable.ColumnStyles.Clear()
        buttonTable.Controls.Clear()

        Dim visibleButtons As New List(Of BaseButton)

        If btnCancel.Visible Then visibleButtons.Add(btnCancel)
        If btnConfirm.Visible Then visibleButtons.Add(btnConfirm)

        Dim count As Integer = visibleButtons.Count

        If count = 0 Then
            buttonTable.Visible = False
            Exit Sub
        End If

        buttonTable.Visible = True
        buttonTable.ColumnCount = count

        For i = 0 To count - 1
            buttonTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F / count))
            buttonTable.Controls.Add(visibleButtons(i), i, 0)
            visibleButtons(i).Dock = DockStyle.Fill
        Next
    End Sub


    Public Sub ShowBaseDialog(owner As Form)
        ownerForm = owner

        Me.StartPosition = FormStartPosition.Manual
        CenterToOwner()

        Me.Show(owner)
        owner.Enabled = False
        Me.BringToFront()

        AddHandler owner.Resize, AddressOf SyncDialogPosition
        AddHandler owner.LocationChanged, AddressOf SyncDialogPosition
    End Sub
    Public Function ShowBaseDialogAsync(owner As Form) As Task(Of DialogResultType)

        Dim tcs As New TaskCompletionSource(Of DialogResultType)

        AddHandler Me.DialogClosed,
        Sub(result)
            tcs.TrySetResult(result)
        End Sub

        ShowBaseDialog(owner)

        Return tcs.Task
    End Function


    Private Sub CenterToOwner()
        If ownerForm Is Nothing OrElse ownerForm.IsDisposed Then Return

        Dim rect = ownerForm.Bounds

        Me.Location = New Point(
        rect.Left + (rect.Width - Me.Width) \ 2,
        rect.Top + (rect.Height - Me.Height) \ 2
    )
    End Sub

    Private Sub SyncDialogPosition(sender As Object, e As EventArgs)
        CenterToOwner()
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)

        If ownerForm IsNot Nothing AndAlso Not ownerForm.IsDisposed Then
            ownerForm.Enabled = True

            RemoveHandler ownerForm.Resize, AddressOf SyncDialogPosition
            RemoveHandler ownerForm.LocationChanged, AddressOf SyncDialogPosition
        End If
    End Sub
End Class



