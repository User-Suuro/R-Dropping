Imports MySql.Data.MySqlClient

Public Class DropOffRoot
    Inherits BasePanel

    Private routeName As String = "Drop-off"

    Private _panelNavTop As Panel
    Private _dropOffRootPanel As Panel
    Public Shared _dropOffRootNav As NavigationManager

    Private _onDeliveryBtn As BaseButton
    Private _completedBtn As BaseButton
    Private _pendingBtn As BaseButton

    Private _dropOffPage As DropOffPage


    Public Sub New()
        Me.Dock = DockStyle.Fill
        root.RootInstance.SetRouteLabel(routeName)
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        _panelNavTop = New Panel()

        With _panelNavTop
            .Dock = DockStyle.Top
            .Height = 44
            .Padding = New Padding(4)
        End With

        _dropOffRootPanel = New Panel()


        With _dropOffRootPanel
            .Dock = DockStyle.Fill
        End With

        _dropOffRootNav = New NavigationManager(_dropOffRootPanel)
        _completedBtn = New BaseButton With {
            .Text = "Settled",
            .Height = 38,
            .Margin = Padding.Empty
        }

        _completedBtn.SetPrimary()

        _pendingBtn = New BaseButton With {
            .Text = "Pending",
            .Height = 38,
            .Margin = Padding.Empty
        }

        _pendingBtn.SetPrimary()

        _onDeliveryBtn = New BaseButton With {
            .Text = "On Delivery",
            .Height = 38,
            .Margin = Padding.Empty
        }

        _onDeliveryBtn.SetPrimary()

        Dim navTable As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 3,
            .RowCount = 1
        }

        navTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33F))
        navTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33F))
        navTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33F))

        navTable.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

        _completedBtn.Dock = DockStyle.Fill
        _onDeliveryBtn.Dock = DockStyle.Fill
        _pendingBtn.Dock = DockStyle.Fill

        navTable.Controls.Add(_pendingBtn, 0, 0)
        navTable.Controls.Add(_onDeliveryBtn, 1, 0)
        navTable.Controls.Add(_completedBtn, 2, 0)

        _panelNavTop.Controls.Add(navTable)

        _dropOffRootNav.GoToPage(New DropOffPage())

        AddHandler _completedBtn.Click, Sub(sender, e)
                                            _dropOffRootNav.GoToPage(New DropOffCompleted())
                                        End Sub
        AddHandler _pendingBtn.Click, Sub(sender, e)
                                          _dropOffRootNav.GoToPage(New DropOffPage())
                                      End Sub

        Me.Controls.Add(_dropOffRootPanel)
        Me.Controls.Add(_panelNavTop)

    End Sub




End Class
