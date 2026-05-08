Public Class root
    Inherits BasePanel
    Public Shared RootInstance As root

    Private TopPanel As Panel
    Private SidebarContainer As Panel
    Private Sidebar As FlowLayoutPanel
    Private MainContent As Panel
    Private RouteLabel As BaseLabel
    Private _activeNavBtn As NavBtn = Nothing

    Private homeBtn As NavBtn
    Private dropOffBtn As NavBtn
    Private employeesBtn As NavBtn
    Private buyerBtn As NavBtn
    Private sellersBtn As NavBtn
    Private courierBtn As NavBtn
    Private pricingBtn As NavBtn
    Private storageBtn As NavBtn
    Private sqlSearchBtn As NavBtn
    Private backupResetBtn As NavBtn

    Public Shared rootNav As NavigationManager

    Public Sub New()
        RootInstance = Me
        Me.Dock = DockStyle.Fill
        Me.BackColor = Color.White
        InitializeUI()
        AddHandler Me.HandleCreated, Sub(s, e) CheckTablesAsync()
    End Sub

    Private Sub InitializeUI()

        ' Top Panel Container
        TopPanel = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 60
        }

        RouteLabel = New BaseLabel

        With RouteLabel
            .SetMedium()
            .Text = "Page"
            .AutoSize = True
            .Left = (TopPanel.ClientSize.Width - RouteLabel.Width) \ 2
            .Top = (TopPanel.ClientSize.Height - RouteLabel.Height) \ 2
        End With

        TopPanel.Controls.Add(RouteLabel)

        AddHandler TopPanel.Resize,
        Sub()
            RouteLabel.Left = (TopPanel.ClientSize.Width - RouteLabel.Width) \ 2
            RouteLabel.Top = (TopPanel.ClientSize.Height - RouteLabel.Height) \ 2
        End Sub


        ' Top Panel Bottom Border

        TopPanel.Controls.Add(New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 1,
            .BackColor = Color.LightGray
        })

        ' Sidebar Container
        SidebarContainer = New Panel With {
            .Dock = DockStyle.Left,
            .Width = 120,
            .Padding = Padding.Empty
        }

        ' Sidebar

        Sidebar = New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .Padding = Padding.Empty,
            .Margin = Padding.Empty
        }

        homeBtn = New NavBtn("Dashboard", SidebarContainer.Width)
        dropOffBtn = New NavBtn("Drop-off", SidebarContainer.Width)
        employeesBtn = New NavBtn("Employees", SidebarContainer.Width)
        buyerBtn = New NavBtn("Buyers", SidebarContainer.Width)
        sellersBtn = New NavBtn("Sellers", SidebarContainer.Width)
        courierBtn = New NavBtn("Courier", SidebarContainer.Width)
        pricingBtn = New NavBtn("Pricing", SidebarContainer.Width)
        storageBtn = New NavBtn("Storage", SidebarContainer.Width)
        sqlSearchBtn = New NavBtn("SQL Search", SidebarContainer.Width)
        backupResetBtn = New NavBtn("Backup & Reset", SidebarContainer.Width)

        Dim exitBtn As New NavBtn("Logout", SidebarContainer.Width)

        exitBtn.Dock = DockStyle.Bottom

        MainContent = New Panel With {
            .Dock = DockStyle.Fill
        }

        rootNav = New NavigationManager(MainContent)



        AddHandler homeBtn.ButtonControl.Click,
        Sub(sender, e)
            showUnavailablePage()
        End Sub

        AddHandler dropOffBtn.ButtonControl.Click,
        Sub(sender, e)
            rootNav.GoToPage(New DropOffRoot())
            SetActiveNav(dropOffBtn)
        End Sub

        AddHandler employeesBtn.ButtonControl.Click,
        Sub(sender, e)
            rootNav.GoToPage(New EmployeePage())
            SetActiveNav(employeesBtn)
        End Sub

        AddHandler buyerBtn.ButtonControl.Click,
        Sub(sender, e)
            rootNav.GoToPage(New BuyerPage())
            SetActiveNav(buyerBtn)
        End Sub

        AddHandler courierBtn.ButtonControl.Click,
        Sub(sender, e)
            rootNav.GoToPage(New CourierPage())
            SetActiveNav(courierBtn)

        End Sub

        AddHandler sellersBtn.ButtonControl.Click,
        Sub(sender, e)
            rootNav.GoToPage(New SellerPage())
            SetActiveNav(sellersBtn)
        End Sub


        AddHandler pricingBtn.ButtonControl.Click,
        Sub(sender, e)
            rootNav.GoToPage(New PricingPage())
            SetActiveNav(pricingBtn)
        End Sub

        AddHandler storageBtn.ButtonControl.Click,
        Sub(sender, e)
            rootNav.GoToPage(New StoragePage())
            SetActiveNav(storageBtn)
        End Sub

        AddHandler sqlSearchBtn.ButtonControl.Click,
              Sub(sender, e)
                  rootNav.GoToPage(New SearchPage())
                  SetActiveNav(sqlSearchBtn)
              End Sub

        AddHandler backupResetBtn.ButtonControl.Click,
              Sub(sender, e)
                  rootNav.GoToPage(New BackupRestorePage())
                  SetActiveNav(backupResetBtn)
              End Sub

        AddHandler exitBtn.ButtonControl.Click,
        Async Sub(sender, e)

            Dim confirm_dlg = New BaseDialog()

            DialogTypes.Apply(confirm_dlg,
                 DialogType.Confirmation,
                 "Confirmation",
                 "Are you sure you want to Logout?")

            If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
                Form1.Instance.ShowLoginScreen()
            End If

        End Sub
        With Sidebar.Controls
            .Add(homeBtn)
            .Add(dropOffBtn)
            .Add(employeesBtn)
            .Add(buyerBtn)
            .Add(sellersBtn)
            .Add(courierBtn)
            .Add(pricingBtn)
            .Add(storageBtn)
            .Add(sqlSearchBtn)
            .Add(backupResetBtn)
        End With



        SidebarContainer.Controls.Add(Sidebar)
        SidebarContainer.Controls.Add(exitBtn)

        ' Sidebar Right Border

        SidebarContainer.Controls.Add(New Panel With {
            .Dock = DockStyle.Right,
            .Width = 1,
            .BackColor = Color.LightGray
        })


        Me.Controls.Add(MainContent)
        Me.Controls.Add(SidebarContainer)
        Me.Controls.Add(TopPanel)

    End Sub
    Private Sub ApplySidebarPermissions()

        Dim role As String =
        session.SessionPosition.Trim().ToLower()

        homeBtn.Visible = False
        employeesBtn.Visible = False
        dropOffBtn.Visible = False
        storageBtn.Visible = False
        sellersBtn.Visible = False
        buyerBtn.Visible = False
        pricingBtn.Visible = False
        storageBtn.Visible = False
        buyerBtn.Visible = False
        courierBtn.Visible = False
        sqlSearchBtn.Visible = False
        backupResetBtn.Visible = False

        Select Case role
            Case "admin"
                employeesBtn.Visible = True
                dropOffBtn.Visible = True
                pricingBtn.Visible = True
                storageBtn.Visible = True
                sellersBtn.Visible = True
                buyerBtn.Visible = True
                courierBtn.Visible = True
                sqlSearchBtn.Visible = True
                backupResetBtn.Visible = True
                rootNav.GoToPage(New EmployeePage())
                SetActiveNav(employeesBtn)

            Case "manager"
                employeesBtn.Visible = True
                storageBtn.Visible = True
                buyerBtn.Visible = True
                sellersBtn.Visible = True
                courierBtn.Visible = True
                rootNav.GoToPage(New EmployeePage())
                SetActiveNav(employeesBtn)

            Case "staff"
                dropOffBtn.Visible = True
                rootNav.GoToPage(New DropOffRoot())
                SetActiveNav(dropOffBtn)
        End Select

    End Sub


    Private Sub showUnavailablePage()
        Dim dlg = New BaseDialog()
        DialogTypes.Apply(dlg,
                 DialogType.Info,
                 "Unavailable Page",
                 "Please wait for updates")
        dlg.ShowBaseDialog(Form1.Instance)
    End Sub

    Public Sub SetRouteLabel(text As String)
        RouteLabel.Text = text.ToUpper()
    End Sub

    Private Sub SetActiveNav(btn As NavBtn)
        If _activeNavBtn IsNot Nothing Then
            _activeNavBtn.IsActive = False
        End If
        _activeNavBtn = btn
        btn.IsActive = True
    End Sub


    Private Async Sub CheckTablesAsync()

        Dim requiredTables As String() = {
            Employee.table_name,
            Buyer.table_name,
            Seller.table_name,
            Courier.table_name,
            Pricing.table_name,
            Storage.table_name,
            Item.table_name,
            Delivery.table_name,
            Stored.table_name
        }

        Dim existingTables As New List(Of String)

        Dim sql As String =
            "SELECT TABLE_NAME
         FROM information_schema.TABLES
         WHERE TABLE_SCHEMA = DATABASE()"

        Using reader = Await ReadQueryAsync(sql)
            If reader IsNot Nothing Then
                While Await reader.ReadAsync()
                    existingTables.Add(reader("TABLE_NAME").ToString().ToLower())
                End While
            End If
        End Using

        Dim missingTables = requiredTables _
            .Where(Function(t) Not existingTables.Contains(t.ToLower())) _
            .ToList()

        If missingTables.Count > 0 Then

            Dim dlg = New BaseDialog()
            DialogTypes.Apply(dlg,
                DialogType.Info,
                "Database Error",
                $"The following tables are missing:{Environment.NewLine}" &
                String.Join(Environment.NewLine, missingTables) &
                $"{Environment.NewLine}{Environment.NewLine}Please restore the database.")

            Await dlg.ShowBaseDialogAsync(Form1.Instance)

            rootNav.GoToPage(New BackupRestorePage())
            SetActiveNav(backupResetBtn)

        Else
            ApplySidebarPermissions()
        End If

    End Sub

End Class

Public Class NavBtn
    Inherits Panel

    Public ReadOnly Property ButtonControl As Button
    Public ReadOnly Property BorderControl As Panel
    Private ReadOnly ActiveBorder As Panel

    Private _isActive As Boolean = False

    Public Property IsActive As Boolean
        Get
            Return _isActive
        End Get
        Set(value As Boolean)
            _isActive = value
            ActiveBorder.Visible = value
            ButtonControl.BackColor = If(value, Color.FromArgb(30, 30, 30), Color.Black)
            ButtonControl.ForeColor = Color.White
        End Set
    End Property

    Public Sub New(text As String, width As Integer)
        Me.Width = width
        Me.Height = 40
        Me.Margin = Padding.Empty
        Me.Padding = Padding.Empty

        ' Active left border indicator
        ActiveBorder = New Panel With {
            .Dock = DockStyle.Left,
            .Width = 3,
            .BackColor = Color.White,
            .Visible = False
        }

        ButtonControl = New Button With {
            .Text = text,
            .Dock = DockStyle.Fill,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.Black,
            .ForeColor = Color.White
        }

        ButtonControl.FlatAppearance.BorderSize = 0

        BorderControl = New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 1,
            .BackColor = Color.LightGray
        }

        AddHandler ButtonControl.EnabledChanged,
        Sub()
            ButtonControl.ForeColor = Color.White
        End Sub

        Me.Controls.Add(ButtonControl)
        Me.Controls.Add(ActiveBorder)
        Me.Controls.Add(BorderControl)
    End Sub





End Class
