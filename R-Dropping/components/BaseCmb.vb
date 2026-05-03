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


' ============================================================
'  BASE COMBO BOX
' ============================================================
Public Class BaseComboBox
    Inherits UserControl
    Implements IValueProvider
    Implements IValidationStyleable

    Public Event SelectedValueChanged As EventHandler
    Public Event ValueChanged As EventHandler Implements IValueProvider.ValueChanged

    Private _comboItems As New List(Of ComboItem)
    Public Property Placeholder As String = "Select an option..."
    Public Property SearchEnabled As Boolean = True

    ''' <summary>
    ''' Override the dropdown panel width. 0 (default) = match the control's own width.
    ''' </summary>
    Public Property DropdownWidth As Integer = 0

    Private _selectedId As String = ""
    Private _selectedDisplay As String = ""
    Private WithEvents _btn As New Guna2Button
    Private _dropdown As ComboDropdownPanel
    Private _label As BaseLabel
    Private _cmbName As String

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

    Public Sub New(cmbName As String)
        Me.AutoSize = True
        Me.AutoSizeMode = AutoSizeMode.GrowAndShrink
        _cmbName = cmbName

        With _btn
            .Dock = DockStyle.Top
            .Height = 36
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
        _btn.ForeColor = If(hasValue,
                            Color.FromArgb(20, 20, 20),
                            Color.FromArgb(150, 150, 150))
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

End Class



Public Class ComboDropdownPanel
    Inherits Guna2Panel
    Implements IMessageFilter

    Public Event ItemSelected(id As String)
    Public Event DropdownClosed()

    Private Const ITEM_H As Integer = 32
    Private Const ITEM_GAP As Integer = 4
    Private Const MAX_VISIBLE As Integer = 6
    Private Const PANEL_PAD_V As Integer = 20
    Private Const SEARCH_H As Integer = 24
    Private Const SEARCH_GAP As Integer = 12
    Private Const EMPTY_H As Integer = 64

    Private Const WM_LBUTTONDOWN As Integer = &H201
    Private Const WM_RBUTTONDOWN As Integer = &H204
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1

    Private ReadOnly _allItems As List(Of ComboItem)
    Private _currentId As String
    Private ReadOnly _disableSearch As Boolean
    Private _isClosing As Boolean = False

    Private WithEvents _search As New Guna2TextBox
    Private _list As New FlowLayoutPanel
    Private _empty As New Label

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

    ' ── IMessageFilter ───────────────────────────────────────
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


    Private Function CalcPanelHeight(visibleCount As Integer) As Integer
        Dim listH = visibleCount * (ITEM_H + ITEM_GAP) - ITEM_GAP
        Return PANEL_PAD_V + SearchBlockH + listH
    End Function


    Private ReadOnly Property ListH As Integer
        Get
            Return Me.Height - PANEL_PAD_V - SearchBlockH
        End Get
    End Property

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
            .AutoScroll = True,
            .Margin = New Padding(0)
        }

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

        flow.Controls.Add(_list)
        flow.Controls.Add(_empty)
        Controls.Add(flow)
    End Sub

    Public Sub OpenDropdown()
        Me.Visible = True
        Populate(_allItems)
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

    Private Sub Populate(items As List(Of ComboItem))
        _list.SuspendLayout()
        _list.Controls.Clear()

        If items Is Nothing OrElse items.Count = 0 Then
            _list.Visible = False
            _empty.Visible = True

            ' Adjust height for empty state
            Me.Height = PANEL_PAD_V + SearchBlockH + EMPTY_H
            ResizeLayout()

            _list.ResumeLayout()
            Return
        End If

        _empty.Visible = False
        _list.Visible = True

        For Each item In items
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

        _list.ResumeLayout()

        AdjustHeight()
        ResizeLayout()
    End Sub


    Private Sub ResizeLayout()
        Dim innerWidth = Me.Width
        _search.Width = innerWidth
        _list.Width = innerWidth
        _list.Height = ListH
        For Each ctrl As Control In _list.Controls
            ctrl.Width = innerWidth
        Next
        _empty.Width = innerWidth
        _empty.Height = EMPTY_H
    End Sub

    Private Sub _search_TextChanged(sender As Object, e As EventArgs) Handles _search.TextChanged
        Dim query = _search.Text.Trim().ToLower()
        Dim filtered = If(String.IsNullOrWhiteSpace(query),
                          _allItems,
                          _allItems.Where(Function(x) x.Display.ToLower().Contains(query)).ToList())
        Populate(filtered)
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        ResizeLayout()
    End Sub

    Public Sub Filter(query As String, Optional currentId As String = Nothing)
        If currentId IsNot Nothing Then _currentId = currentId
        Dim filtered = If(String.IsNullOrWhiteSpace(query),
                          _allItems,
                          _allItems.Where(Function(x) x.Display.ToLower().Contains(query.ToLower())).ToList())
        Populate(filtered)
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            Application.RemoveMessageFilter(Me)
        End If
        MyBase.Dispose(disposing)
    End Sub

    Private Sub AdjustHeight()
        Dim contentHeight As Integer = 18

        For Each ctrl As Control In _list.Controls
            contentHeight += ctrl.Height + ctrl.Margin.Bottom
        Next

        If _list.Controls.Count > 0 Then
            contentHeight -= ITEM_GAP ' remove last gap
        End If

        ' Cap visible items
        Dim maxHeight = MAX_VISIBLE * (ITEM_H + ITEM_GAP) - ITEM_GAP
        contentHeight = Math.Min(contentHeight, maxHeight)

        Dim totalHeight =
            PANEL_PAD_V +
            SearchBlockH +
            contentHeight

        ' Add scrollbar allowance if needed
        If contentHeight >= maxHeight Then
            totalHeight += SystemInformation.HorizontalScrollBarHeight
        End If

        Me.Height = Math.Max(totalHeight, 80) ' enforce minimum
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
            .Height = 36
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