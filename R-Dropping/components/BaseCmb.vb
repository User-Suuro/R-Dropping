Imports Guna.UI2.WinForms

Public Class ComboItem
    Public Property Id As String
    Public Property Display As String

    Public Sub New(id As String, display As String)
        Me.Id = id
        Me.Display = display
    End Sub

    Public Shared Widening Operator CType(value As String) As ComboItem
        Return New ComboItem(value, value)
    End Operator

    Public Overrides Function ToString() As String
        Return Display
    End Function
End Class



Public Class BaseComboBox
    Inherits UserControl
    Implements IValueProvider
    Implements IValidationStyleable

    Public Event SelectedValueChanged As EventHandler
    Public Event ValueChanged As EventHandler Implements IValueProvider.ValueChanged

    Private _comboItems As New List(Of ComboItem)
    Public Property Placeholder As String = "Select an option..."
    Public Property SearchEnabled As Boolean = True

    Public Property DropdownWidth As Integer = 0

    Private _selectedId As String = ""
    Private _selectedDisplay As String = ""
    Private WithEvents _btn As New Guna2Button
    Private _dropdown As ComboDropdownPanel
    Private _label As BaseLabel
    Private _cmbName As String
    Private _clearBtn As Guna2Button
    Private _clearable As Boolean = False

    Public WriteOnly Property Items As List(Of String)
        Set(value As List(Of String))
            _comboItems = value.Select(Function(s) CType(s, ComboItem)).ToList()
        End Set
    End Property

    Public WriteOnly Property ComboItems As List(Of ComboItem)
        Set(value As List(Of ComboItem))
            _comboItems = value
        End Set
    End Property

    Public ReadOnly Property Value As String Implements IValueProvider.Value
        Get
            Return _selectedId
        End Get
    End Property

    Public Property SelectedValue As String
        Get
            Return _selectedId
        End Get
        Set(value As String)
            _selectedId = value
            Dim match = _comboItems.FirstOrDefault(Function(x) x.Id = value)
            _selectedDisplay = If(match IsNot Nothing, match.Display, value)
            UpdateButtonAppearance()
            RaiseEvent SelectedValueChanged(Me, EventArgs.Empty)
            RaiseEvent ValueChanged(Me, EventArgs.Empty)
        End Set
    End Property

    Public Sub OnValidationError() Implements IValidationStyleable.OnValidationError
        _btn.BorderColor = Color.Red
    End Sub

    Public Sub OnValidationClear() Implements IValidationStyleable.OnValidationClear
        _btn.BorderColor = Color.FromArgb(220, 220, 220)
    End Sub

    Public Sub SetClearable()
        If _clearable Then Return
        _clearable = True

        _clearBtn = New Guna2Button With {
        .Size = New Size(22, 22),
        .Text = "✕",
        .FillColor = Color.Transparent,
        .ForeColor = Color.FromArgb(160, 160, 160),
        .BorderThickness = 0,
        .Font = New Font("Segoe UI", 7.5F),
        .Cursor = Cursors.Hand,
        .Visible = False
    }

        _clearBtn.HoverState.FillColor = Color.FromArgb(235, 235, 235)
        _clearBtn.HoverState.ForeColor = Color.FromArgb(60, 60, 60)

        AddHandler _clearBtn.Click, Sub(s, e) ClearSelection()

        Me.Controls.Add(_clearBtn)
        _clearBtn.BringToFront()

        PositionClearButton()

        AddHandler Me.Resize, Sub(s, e) PositionClearButton()
        AddHandler _btn.SizeChanged, Sub(s, e) PositionClearButton()
    End Sub

    Private Sub PositionClearButton()
        If _clearBtn Is Nothing Then Return
        _clearBtn.Left = Me.Width - _clearBtn.Width - 4
        _clearBtn.Top = _btn.Top + (_btn.Height - _clearBtn.Height) \ 2
    End Sub



    Public Sub New(cmbName As String)
        Me.AutoSize = True
        Me.AutoSizeMode = AutoSizeMode.GrowAndShrink
        _cmbName = cmbName

        With _btn
            .Dock = DockStyle.Top
            .Height = 32
            .FillColor = Color.White
            .ForeColor = Color.FromArgb(150, 150, 150)
            .BorderColor = Color.FromArgb(220, 220, 220)
            .BorderRadius = 4
            .BorderThickness = 1
            .Font = New Font("Segoe UI", 9.0F)
            .TextAlign = HorizontalAlignment.Left
            .HoverState.FillColor = Color.FromArgb(248, 250, 252)
            .Cursor = Cursors.Hand
        End With

        _label = New BaseLabel
        With _label
            .Text = cmbName
            .Dock = DockStyle.Top
            .SetSmall()
            .Padding = New Padding(0, 0, 0, 4)
        End With

        UpdateButtonAppearance()
        Me.Controls.Add(_btn)
        Me.Controls.Add(_label)
    End Sub

    Private Sub UpdateButtonAppearance()
        Dim hasValue = Not String.IsNullOrWhiteSpace(_selectedId)
        _btn.Text = If(hasValue, "  " & _selectedDisplay, Placeholder)
        _btn.ForeColor = If(hasValue, Color.FromArgb(20, 20, 20), Color.FromArgb(150, 150, 150))

        If _clearable AndAlso _clearBtn IsNot Nothing Then
            _clearBtn.Visible = hasValue
        End If
    End Sub

    Private Sub _btn_Click(sender As Object, e As EventArgs) Handles _btn.Click
        If _dropdown IsNot Nothing Then
            _dropdown.CloseDropdown()
            _dropdown = Nothing
            Return
        End If

        Dim parentForm = Me.FindForm()
        If parentForm Is Nothing Then Return

        _dropdown = New ComboDropdownPanel(_comboItems, _selectedId, Not SearchEnabled)
        _dropdown.Width = If(DropdownWidth > 0, DropdownWidth, Me.Width)

        Dim screenPoint = Me.Parent.PointToScreen(Me.Location)
        Dim formPoint = parentForm.PointToClient(screenPoint)
        _dropdown.Location = New Point(formPoint.X - 4, formPoint.Y + Me.Height + 4)

        AddHandler _dropdown.ItemSelected,
            Sub(selectedId As String)
                SelectedValue = selectedId
            End Sub

        AddHandler _dropdown.DropdownClosed,
            Sub()
                _dropdown = Nothing
            End Sub

        parentForm.Controls.Add(_dropdown)
        _dropdown.BringToFront()
        _dropdown.OpenDropdown()
    End Sub

    Public Sub ClearSelection()
        SelectedValue = String.Empty
    End Sub

    Public Sub SetValue(value As String)
        Dim byId = _comboItems.FirstOrDefault(Function(x) x.Id = value)
        Dim byDisplay = _comboItems.FirstOrDefault(Function(x) x.Display = value)
        Dim match = If(byId, byDisplay)

        If match IsNot Nothing Then
            SelectedValue = match.Id
        Else
            SelectedValue = String.Empty
        End If
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()
        Me.Name = _cmbName
        Me.ResumeLayout(False)
    End Sub

    Private Sub BaseComboBox_Load(sender As Object, e As EventArgs) Handles MyBase.Load
    End Sub

    Public Function GetDisplayText() As String
        If Not String.IsNullOrWhiteSpace(_selectedDisplay) Then
            Return _selectedDisplay
        End If

        Dim match = _comboItems.FirstOrDefault(Function(x) x.Id = _selectedId)
        If match IsNot Nothing Then
            Return match.Display
        End If

        Return String.Empty
    End Function
End Class



Public Class ComboDropdownPanel
    Inherits Guna2Panel
    Implements IMessageFilter

    Public Event ItemSelected(id As String)
    Public Event DropdownClosed()

    Private Const ITEM_H As Integer = 32
    Private Const ITEM_GAP As Integer = 4
    Private Const PANEL_PAD_V As Integer = 20
    Private Const SEARCH_H As Integer = 24
    Private Const SEARCH_GAP As Integer = 12
    Private Const EMPTY_H As Integer = 64
    Private Const PAGINATION_PANEL_HEIGHT As Integer = 38
    Private _minWidth As Integer = 300

    Private Const WM_LBUTTONDOWN As Integer = &H201
    Private Const WM_RBUTTONDOWN As Integer = &H204
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1

    Private ReadOnly _allItems As List(Of ComboItem)
    Private _currentId As String
    Private ReadOnly _disableSearch As Boolean
    Private _isClosing As Boolean = False

    Private _filteredItems As List(Of ComboItem)
    Private _currentPage As Integer = 1
    Private _pageSize As Integer = 10

    Private WithEvents _search As New Guna2TextBox
    Private _list As New FlowLayoutPanel
    Private _empty As New Label

    ' Pagination controls  (initialised in BuildPaginationPanel, but guarded against Nothing)
    Private _paginationPanel As Panel
    Private _pageInfoLabel As Label
    Private _firstPageBtn As Guna2Button
    Private _prevPageBtn As Guna2Button
    Private _nextPageBtn As Guna2Button
    Private _lastPageBtn As Guna2Button

    Public Property MinWidth As Integer
        Get
            Return _minWidth
        End Get
        Set(value As Integer)
            If value < 1 Then value = 1
            _minWidth = value

            Me.Width = Math.Max(Me.Width, _minWidth)
        End Set
    End Property


    Public Shadows Property Width As Integer
        Get
            Return MyBase.Width
        End Get
        Set(value As Integer)
            MyBase.Width = Math.Max(value, _minWidth)
        End Set
    End Property

    Protected Overrides Sub SetBoundsCore(x As Integer, y As Integer, width As Integer, height As Integer, specified As BoundsSpecified)
        width = Math.Max(width, _minWidth)
        MyBase.SetBoundsCore(x, y, width, height, specified)
    End Sub

    Public Property PageSize As Integer
        Get
            Return _pageSize
        End Get
        Set(value As Integer)
            If value < 1 Then value = 1
            _pageSize = value
            If _filteredItems IsNot Nothing Then
                _currentPage = Math.Min(_currentPage, TotalPages)
                PopulateCurrentPage()
            End If
        End Set
    End Property

    Private ReadOnly Property TotalPages As Integer
        Get
            If _filteredItems Is Nothing OrElse _filteredItems.Count = 0 Then Return 1
            Return CInt(Math.Ceiling(_filteredItems.Count / CDbl(_pageSize)))
        End Get
    End Property

    Public Sub New(items As List(Of ComboItem), currentId As String,
                   Optional disableSearch As Boolean = False)
        _allItems = items
        _currentId = currentId
        _disableSearch = disableSearch

        Me.FillColor = Color.White
        Me.BorderRadius = 4
        Me.BorderThickness = 1
        Me.BorderColor = Color.FromArgb(220, 220, 220)
        Me.Padding = New Padding(8)
        Me.Visible = False

        BuildUI()
    End Sub

    Public Function PreFilterMessage(ByRef m As Message) As Boolean _
        Implements IMessageFilter.PreFilterMessage

        If m.Msg = WM_LBUTTONDOWN OrElse
           m.Msg = WM_RBUTTONDOWN OrElse
           m.Msg = WM_NCLBUTTONDOWN Then

            If Not Me.RectangleToScreen(Me.ClientRectangle).Contains(Cursor.Position) Then
                CloseDropdown()
            End If
        End If
        Return False
    End Function

    Private ReadOnly Property SearchBlockH As Integer
        Get
            Return If(_disableSearch, 0, SEARCH_H + SEARCH_GAP)
        End Get
    End Property

    Private ReadOnly Property PaginationBlockH As Integer
        Get
            If _paginationPanel IsNot Nothing AndAlso
               _filteredItems IsNot Nothing AndAlso _filteredItems.Count > _pageSize Then
                Return PAGINATION_PANEL_HEIGHT
            End If
            Return 0
        End Get
    End Property

    ' ── UI construction ─────────────────────────────────────
    Private Sub BuildUI()
        Dim flow As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .AutoScroll = False,
            .Padding = New Padding(0),
            .BackColor = Color.White
        }

        If Not _disableSearch Then
            With _search
                .Width = Me.Width
                .Height = SEARCH_H
                .PlaceholderText = "Search..."
                .FillColor = Color.FromArgb(248, 250, 252)
                .BorderRadius = 4
                .BorderColor = Color.FromArgb(220, 220, 220)
                .FocusedState.BorderColor = Color.FromArgb(99, 102, 241)
                .Font = New Font("Segoe UI", 9.5F)
                .Margin = New Padding(0, 0, 24, SEARCH_GAP)
                .Anchor = AnchorStyles.Left Or AnchorStyles.Right
            End With
            flow.Controls.Add(_search)
        End If

        _list = New FlowLayoutPanel With {
            .Width = Me.Width,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .AutoScroll = False,
            .Margin = New Padding(0)
        }
        flow.Controls.Add(_list)

        ' Build pagination bar (may become Nothing if an exception occurs – guarded later)
        BuildPaginationPanel()
        flow.Controls.Add(_paginationPanel)

        _empty = New Label With {
            .Text = "No results found.",
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = Color.FromArgb(150, 150, 150),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Width = Me.Width,
            .BackColor = Color.White,
            .Height = EMPTY_H,
            .Visible = False
        }
        flow.Controls.Add(_empty)

        Controls.Add(flow)
    End Sub

    Private Sub BuildPaginationPanel()

        If _paginationPanel IsNot Nothing Then Return

        _paginationPanel = New Panel With {
        .Height = PAGINATION_PANEL_HEIGHT,
        .Width = Me.Width - Me.Padding.Horizontal,   ' use client width
        .BackColor = Color.White,
        .Visible = False,
        .Margin = New Padding(0, 4, 0, 0)           ' no right margin
    }

        _pageInfoLabel = New Label With {
        .Text = "Page 1 of 1  (0 items)",
        .Font = New Font("Segoe UI", 8.5F),
        .ForeColor = Color.FromArgb(100, 100, 100),
        .TextAlign = ContentAlignment.MiddleCenter,
        .Dock = DockStyle.Fill
    }

        _firstPageBtn = CreateNavButton("«")
        _prevPageBtn = CreateNavButton("‹")
        _nextPageBtn = CreateNavButton("›")
        _lastPageBtn = CreateNavButton("»")

        AddHandler _firstPageBtn.Click, Sub(s, e) GoToPage(1)
        AddHandler _prevPageBtn.Click, Sub(s, e) GoToPage(_currentPage - 1)
        AddHandler _nextPageBtn.Click, Sub(s, e) GoToPage(_currentPage + 1)
        AddHandler _lastPageBtn.Click, Sub(s, e) GoToPage(TotalPages)

        Dim table As New TableLayoutPanel With {
        .Dock = DockStyle.Fill,
        .ColumnCount = 5,
        .RowCount = 1,
        .BackColor = Color.White,
        .Margin = New Padding(0)
    }

        table.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 28))
        table.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 28))
        table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        table.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 28))
        table.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 28))
        table.RowStyles.Add(New RowStyle(SizeType.Absolute, 28))

        table.Controls.Add(_firstPageBtn, 0, 0)
        table.Controls.Add(_prevPageBtn, 1, 0)
        table.Controls.Add(_pageInfoLabel, 2, 0)
        table.Controls.Add(_nextPageBtn, 3, 0)
        table.Controls.Add(_lastPageBtn, 4, 0)          ' ← ADDED

        _paginationPanel.Controls.Add(table)
    End Sub

    Private Function CreateNavButton(text As String) As Guna2Button
        Return New Guna2Button With {
            .Text = text,
            .Width = 24,
            .Height = 24,
            .FillColor = Color.White,
            .ForeColor = Color.FromArgb(80, 80, 80),
            .BorderThickness = 0,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .TextAlign = HorizontalAlignment.Center,
            .Cursor = Cursors.Hand,
            .Margin = New Padding(2)
        }
    End Function

    ' ── Public entry points ─────────────────────────────────
    Public Sub OpenDropdown()
        Me.Visible = True
        _filteredItems = New List(Of ComboItem)(_allItems)
        _currentPage = 1
        PopulateCurrentPage()
        If Not _disableSearch Then _search.Focus()
        Application.AddMessageFilter(Me)
    End Sub

    Public Sub CloseDropdown()
        If _isClosing Then Return
        _isClosing = True
        Application.RemoveMessageFilter(Me)
        If Me.Parent IsNot Nothing Then
            Me.Parent.Controls.Remove(Me)
        End If
        RaiseEvent DropdownClosed()
        Me.Dispose()
    End Sub

    Public Sub Filter(query As String, Optional currentId As String = Nothing)
        If currentId IsNot Nothing Then _currentId = currentId
        FilterItems(query)
        _currentPage = 1
        PopulateCurrentPage()
    End Sub

    Private Sub FilterItems(query As String)
        If String.IsNullOrWhiteSpace(query) Then
            _filteredItems = New List(Of ComboItem)(_allItems)
        Else
            Dim q = query.ToLower()
            _filteredItems = _allItems.
                Where(Function(x) x.Display.ToLower().Contains(q)).
                ToList()
        End If
    End Sub

    ' ── Pagination logic ────────────────────────────────────
    Private Sub GoToPage(page As Integer)
        Dim total = TotalPages
        Dim newPage = Math.Max(1, Math.Min(page, total))
        If newPage = _currentPage Then Return
        _currentPage = newPage
        PopulateCurrentPage()
    End Sub

    Private Sub PopulateCurrentPage()
        If _filteredItems Is Nothing Then Return

        _list.SuspendLayout()
        _list.Controls.Clear()

        Dim totalItems = _filteredItems.Count
        Dim totalPageCount As Integer = TotalPages

        If totalItems = 0 Then
            _list.Visible = False
            If _paginationPanel IsNot Nothing Then _paginationPanel.Visible = False
            _empty.Visible = True
            Me.Height = PANEL_PAD_V + SearchBlockH + EMPTY_H
            ResizeLayout()
            _list.ResumeLayout()
            Return
        End If

        _empty.Visible = False
        _list.Visible = True

        ' If pagination panel is missing (should never happen), fall back to showing all items
        Dim showPagination As Boolean = False
        If _paginationPanel IsNot Nothing AndAlso totalItems > _pageSize Then
            showPagination = True
        End If

        Dim startIdx As Integer, count As Integer
        If showPagination Then
            startIdx = (_currentPage - 1) * _pageSize
            count = Math.Min(_pageSize, totalItems - startIdx)
        Else
            startIdx = 0
            count = totalItems
        End If

        Dim pageItems = _filteredItems.GetRange(startIdx, count)

        For Each item In pageItems
            Dim isSelected = (item.Id = _currentId)
            Dim btn As New Guna2Button With {
                .Text = If(isSelected, "  ✓  " & item.Display, "     " & item.Display),
                .Height = ITEM_H,
                .Width = Me.Width,
                .FillColor = If(isSelected, Color.FromArgb(238, 242, 255), Color.White),
                .ForeColor = If(isSelected, Color.FromArgb(79, 70, 229), Color.FromArgb(30, 41, 59)),
                .BorderThickness = 0,
                .Font = New Font("Segoe UI", 9.5F,
                                 If(isSelected, FontStyle.Bold, FontStyle.Regular)),
                .TextAlign = HorizontalAlignment.Left,
                .Cursor = Cursors.Hand,
                .Tag = item,
                .Margin = New Padding(0, 0, 0, ITEM_GAP)
            }

            btn.HoverState.FillColor = Color.FromArgb(238, 242, 255)
            btn.HoverState.ForeColor = Color.FromArgb(79, 70, 229)

            AddHandler btn.Click,
                Sub(sender As Object, e As EventArgs)
                    Dim selected = DirectCast(btn.Tag, ComboItem)
                    RaiseEvent ItemSelected(selected.Id)
                    CloseDropdown()
                End Sub

            _list.Controls.Add(btn)
        Next

        Dim listHeight = count * (ITEM_H + ITEM_GAP) - ITEM_GAP
        _list.Height = listHeight

        If showPagination Then
            _paginationPanel.Visible = True
            _pageInfoLabel.Text = String.Format("Page {0} of {1}  ({2} items)", _currentPage, totalPageCount, totalItems)
            _firstPageBtn.Enabled = _currentPage > 1
            _prevPageBtn.Enabled = _currentPage > 1
            _nextPageBtn.Enabled = _currentPage < totalPageCount
            _lastPageBtn.Enabled = _currentPage < totalPageCount
        Else
            If _paginationPanel IsNot Nothing Then _paginationPanel.Visible = False
        End If

        _list.ResumeLayout()
        Me.Height = CalculateDropdownHeight(listHeight, showPagination)
        ResizeLayout()
    End Sub

    Private Function CalculateDropdownHeight(listHeight As Integer, paginationVisible As Boolean) As Integer
        Dim h = PANEL_PAD_V + SearchBlockH + listHeight
        If paginationVisible Then h += PAGINATION_PANEL_HEIGHT
        Return Math.Max(h, 80)
    End Function

    Private Sub ResizeLayout()
        Dim innerWidth = Me.Width
        _search.Width = innerWidth
        _list.Width = innerWidth
        For Each ctrl As Control In _list.Controls
            ctrl.Width = innerWidth
        Next


        Dim clientW = innerWidth - Me.Padding.Horizontal
        If _paginationPanel IsNot Nothing Then
            _paginationPanel.Width = clientW
        End If

        _empty.Width = innerWidth
        _empty.Height = EMPTY_H
    End Sub

    Private Sub _search_TextChanged(sender As Object, e As EventArgs) Handles _search.TextChanged
        FilterItems(_search.Text)
        _currentPage = 1
        PopulateCurrentPage()
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        ResizeLayout()
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            Application.RemoveMessageFilter(Me)
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class


' ============================================================
'  INPUT COMBO BOX
' ============================================================
Public Class InputComboBox
    Inherits UserControl
    Implements IValueProvider
    Implements IValidationStyleable

    Public Event SelectedValueChanged As EventHandler
    Public Event ValueChanged As EventHandler Implements IValueProvider.ValueChanged

    Private _comboItems As New List(Of ComboItem)
    Public Property Placeholder As String = "Type or select..."
    Public Property AllowFreeText As Boolean = True


    Public Property DropdownWidth As Integer = 0

    Private _selectedId As String = ""
    Private WithEvents _txt As New Guna2TextBox
    Private _dropdown As ComboDropdownPanel
    Private _label As BaseLabel
    Private _cmbName As String
    Private _suppressChange As Boolean = False

    Public WriteOnly Property Items As List(Of String)
        Set(value As List(Of String))
            _comboItems = value.Select(Function(s) CType(s, ComboItem)).ToList()
        End Set
    End Property

    Public WriteOnly Property ComboItems As List(Of ComboItem)
        Set(value As List(Of ComboItem))
            _comboItems = value
        End Set
    End Property

    Public ReadOnly Property Value As String Implements IValueProvider.Value
        Get
            Return _selectedId
        End Get
    End Property

    Public Property SelectedValue As String
        Get
            Return _selectedId
        End Get
        Set(value As String)
            _selectedId = value
            Dim match = _comboItems.FirstOrDefault(Function(x) x.Id = value)
            Dim displayText = If(match IsNot Nothing, match.Display, value)
            _suppressChange = True
            _txt.Text = displayText
            _suppressChange = False
            RaiseEvent SelectedValueChanged(Me, EventArgs.Empty)
            RaiseEvent ValueChanged(Me, EventArgs.Empty)
        End Set
    End Property

    Public Sub OnValidationError() Implements IValidationStyleable.OnValidationError
        _txt.BorderColor = Color.Red
    End Sub

    Public Sub OnValidationClear() Implements IValidationStyleable.OnValidationClear
        _txt.BorderColor = Color.FromArgb(220, 220, 220)
    End Sub

    Public Sub New(cmbName As String)
        Me.AutoSize = True
        Me.AutoSizeMode = AutoSizeMode.GrowAndShrink
        _cmbName = cmbName

        With _txt
            .Dock = DockStyle.Top
            .Height = 32
            .FillColor = Color.White
            .ForeColor = Color.FromArgb(20, 20, 20)
            .BorderColor = Color.FromArgb(220, 220, 220)
            .BorderRadius = 4
            .BorderThickness = 1
            .Font = New Font("Segoe UI", 9.0F)
            .PlaceholderText = Placeholder
            .FocusedState.BorderColor = Color.FromArgb(99, 102, 241)
        End With

        _label = New BaseLabel With {
            .Text = cmbName,
            .Dock = DockStyle.Top
        }
        _label.SetSmall()
        _label.Padding = New Padding(0, 0, 0, 4)

        Me.Controls.Add(_txt)
        Me.Controls.Add(_label)
    End Sub

    Private Sub _txt_GotFocus(sender As Object, e As EventArgs) Handles _txt.GotFocus
        If _dropdown Is Nothing Then OpenDropdown()
    End Sub

    Private Sub _txt_Click(sender As Object, e As EventArgs) Handles _txt.Click
        If _dropdown Is Nothing Then OpenDropdown()
    End Sub

    Private Sub _txt_TextChanged(sender As Object, e As EventArgs) Handles _txt.TextChanged
        If _suppressChange Then Return

        Dim query = _txt.Text
        Dim queryLower = query.ToLower()

        Dim exactMatch = _comboItems.FirstOrDefault(Function(x) x.Display.ToLower() = queryLower)

        If exactMatch IsNot Nothing Then
            Dim caret = _txt.SelectionStart

            _selectedId = exactMatch.Id
            _suppressChange = True
            _txt.Text = exactMatch.Display
            _suppressChange = False

            If _dropdown IsNot Nothing Then
                _dropdown.CloseDropdown()
            End If

            _txt.SelectionStart = Math.Min(caret, _txt.Text.Length)

            RaiseEvent SelectedValueChanged(Me, EventArgs.Empty)
            RaiseEvent ValueChanged(Me, EventArgs.Empty)
            Return
        End If

        If Not queryLower.Equals(_selectedId.ToLower().Trim()) Then
            _selectedId = If(AllowFreeText, query, String.Empty)
            RaiseEvent ValueChanged(Me, EventArgs.Empty)
        End If

        If _dropdown Is Nothing Then
            OpenDropdown()
        Else
            _dropdown.Filter(query, _selectedId)
        End If
    End Sub

    Private Sub _txt_LostFocus(sender As Object, e As EventArgs) Handles _txt.LostFocus
        BeginInvoke(Sub()
                        If _dropdown Is Nothing Then Return

                        Dim f = FindForm()
                        If f Is Nothing Then
                            _dropdown.CloseDropdown()
                            Return
                        End If

                        Dim focused As Control = f.ActiveControl
                        Dim c As Control = focused
                        While c IsNot Nothing
                            If c Is _dropdown Then Return
                            c = c.Parent
                        End While

                        _dropdown.CloseDropdown()
                    End Sub)
    End Sub

    Private Sub OpenDropdown()
        Dim parentForm = Me.FindForm()
        If parentForm Is Nothing Then Return

        _dropdown = New ComboDropdownPanel(_comboItems, _selectedId, disableSearch:=True)
        _dropdown.Width = If(DropdownWidth > 0, DropdownWidth, Me.Width)

        Dim screenPt = Me.Parent.PointToScreen(Me.Location)
        Dim formPt = parentForm.PointToClient(screenPt)
        _dropdown.Location = New Point(formPt.X - 4, formPt.Y + Me.Height + 4)

        AddHandler _dropdown.ItemSelected,
            Sub(selectedId As String)
                SelectedValue = selectedId
                _txt.Focus()
            End Sub

        AddHandler _dropdown.DropdownClosed,
            Sub()
                _dropdown = Nothing
            End Sub

        parentForm.Controls.Add(_dropdown)
        _dropdown.BringToFront()
        _dropdown.OpenDropdown()

        If Not String.IsNullOrWhiteSpace(_txt.Text) Then
            _dropdown.Filter(_txt.Text)
        End If
    End Sub

    Public Function GetValue() As String
        Return If(_selectedId IsNot Nothing, _selectedId.Trim(), String.Empty)
    End Function

    Public Function GetValue(fallback As String) As String
        Dim v = GetValue()
        Return If(String.IsNullOrWhiteSpace(v), fallback, v)
    End Function

    Public Sub ClearSelection()
        SelectedValue = String.Empty
    End Sub

    Public Sub SetValue(value As String)
        Dim byId = _comboItems.FirstOrDefault(Function(x) x.Id = value)
        Dim byDisplay = _comboItems.FirstOrDefault(Function(x) x.Display = value)
        Dim match = If(byId, byDisplay)

        If match IsNot Nothing Then
            SelectedValue = match.Id
        ElseIf AllowFreeText Then
            SelectedValue = value
        Else
            SelectedValue = String.Empty
        End If
    End Sub

End Class