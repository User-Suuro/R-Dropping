Imports Guna.UI2.WinForms
Public Enum ButtonVisibility
    Always          ' Always visible
    OnSelection     ' Show only when a row is selected
    OnNoSelection   ' Show only when nothing is selected 
End Enum

Public Class BaseDGV
    Inherits BasePanel

    Private ReadOnly _dgv As New Guna2DataGridView()

    Private Structure ActionButtonEntry
        Public Button As BaseButton
        Public Visibility As ButtonVisibility
    End Structure

    Private ReadOnly _actionButtons As New List(Of ActionButtonEntry)()


    ' ── Toolbar (top)
    Private _toolbarPanel As Panel
    Private _toolbarLayout As TableLayoutPanel
    Private _searchInput As Guna2TextBox
    Private _searchBtn As BaseButton

    Private _actionButtonsPanel As FlowLayoutPanel

    ' ── Pagination (bottom)
    Private _paginationPanel As Panel
    Private _paginationLayout As TableLayoutPanel
    Private _pageSizeInput As Guna2TextBox
    Private _pageInfoLabel As Label
    Private _firstPageBtn As BaseButton
    Private _prevPageBtn As BaseButton
    Private _nextPageBtn As BaseButton
    Private _lastPageBtn As BaseButton
    Private _noResultsLabel As Label

    ' ── States
    Private _originalDataTable As DataTable = Nothing
    Private _filteredDataTable As DataTable = Nothing
    Private _currentPage As Integer = 1
    Private _pageSize As Integer = 10
    Private _isPaging As Boolean = False
    Private _isFiltering As Boolean = False

    Public Event AfterPageApplied As EventHandler

    ' ── Properties 
    Public ReadOnly Property DataGridView As Guna2DataGridView
        Get
            Return _dgv
        End Get
    End Property

    Public ReadOnly Property SearchInput As Guna2TextBox
        Get
            Return _searchInput
        End Get
    End Property

    Public ReadOnly Property SearchButton As BaseButton
        Get
            Return _searchBtn
        End Get
    End Property


    Public ReadOnly Property CurrentPage As Integer
        Get
            Return _currentPage
        End Get
    End Property

    Public ReadOnly Property PageSize As Integer
        Get
            Return _pageSize
        End Get
    End Property

    Public ReadOnly Property TotalPages As Integer
        Get
            If _filteredDataTable Is Nothing OrElse _filteredDataTable.Rows.Count = 0 Then Return 1
            Return CInt(Math.Ceiling(_filteredDataTable.Rows.Count / CType(_pageSize, Double)))
        End Get
    End Property

    Public Sub New()
        MyBase.New()
        Me.Dock = DockStyle.Fill
        InitializeUI()
        SetupEventHandlers()
    End Sub

    ' ── UI Setup 
    Private Sub InitializeUI()

        ' 
        '  TOP TOOLBAR
        ' 
        _toolbarPanel = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 60,
            .BackColor = Colors.Primary
        }

        _toolbarLayout = New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 3,
            .RowCount = 1,
            .BackColor = Colors.Primary,
            .Margin = Padding.Empty,
            .Padding = New Padding(10, 11, 10, 11),
            .AutoSize = False
        }
        _toolbarLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        _toolbarLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        _toolbarLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        _toolbarLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

        _searchInput = New Guna2TextBox With {
            .PlaceholderText = "Search…",
            .Dock = DockStyle.Top,
            .Font = New Font(Strings.FONT_FAMILY, Dimen.LABEL_SM),
            .FillColor = Color.White,
            .ForeColor = Colors.LblPrimary,
            .BorderRadius = 4,
            .Margin = New Padding(0, 0, 8, 0),
            .Height = 38
        }

        _searchBtn = New BaseButton With {
            .Text = "Search",
            .Width = 105,
            .Height = 38,
            .Margin = New Padding(0, 0, 10, 0),
            .Anchor = AnchorStyles.Left Or AnchorStyles.Top
        }
        _searchBtn.SetPrimary()

        _actionButtonsPanel = New FlowLayoutPanel With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .FlowDirection = FlowDirection.RightToLeft,
            .WrapContents = False,
            .Margin = Padding.Empty,
            .Padding = Padding.Empty,
            .BackColor = Colors.Primary,
            .Anchor = AnchorStyles.Left Or AnchorStyles.Top
        }


        _toolbarLayout.Controls.Add(_searchInput, 0, 0)
        _toolbarLayout.Controls.Add(_searchBtn, 1, 0)
        _toolbarLayout.Controls.Add(_actionButtonsPanel, 2, 0)
        _toolbarPanel.Controls.Add(_toolbarLayout)

        ' 
        '  BOTTOM PAGINATION TOOLBAR
        ' 
        _paginationPanel = New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 52,
            .BackColor = Colors.Primary
        }

        _paginationLayout = New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 3,
            .RowCount = 1,
            .BackColor = Colors.Primary,
            .Margin = Padding.Empty,
            .Padding = New Padding(10, 7, 10, 7),
            .AutoSize = False
        }
        _paginationLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33))
        _paginationLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 34))
        _paginationLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33))
        _paginationLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

        ' Left: rows-per-page
        Dim leftPanel As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .BackColor = Colors.Primary,
            .Margin = Padding.Empty,
            .Padding = Padding.Empty
        }

        Dim rowsLabel As New Label With {
            .Text = "Rows per page:",
            .AutoSize = False,
            .Width = 105,
            .Height = 38,
            .Font = New Font(Strings.FONT_FAMILY, Dimen.LABEL_SM),
            .ForeColor = Colors.OnPrimary,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Margin = New Padding(0, 0, 6, 0)
        }

        _pageSizeInput = New Guna2TextBox With {
            .Text = _pageSize.ToString(),
            .Width = 55,
            .Height = 32,
            .Font = New Font(Strings.FONT_FAMILY, Dimen.LABEL_SM),
            .FillColor = Color.White,
            .ForeColor = Colors.LblPrimary,
            .BorderRadius = 4,
            .TextAlign = HorizontalAlignment.Center,
            .Margin = Padding.Empty
        }

        leftPanel.Controls.Add(rowsLabel)
        leftPanel.Controls.Add(_pageSizeInput)

        ' Center: page info
        _pageInfoLabel = New Label With {
            .Text = "Page 1 of 1  (0 items)",
            .Dock = DockStyle.Fill,
            .Font = New Font(Strings.FONT_FAMILY, Dimen.LABEL_SM),
            .ForeColor = Colors.OnPrimary,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Margin = Padding.Empty
        }

        ' Right: nav buttons
        Dim rightPanel As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.RightToLeft,
            .WrapContents = False,
            .BackColor = Colors.Primary,
            .Margin = Padding.Empty,
            .Padding = Padding.Empty
        }

        _lastPageBtn = New BaseButton With {
            .Text = "»",
            .Width = 38,
            .Height = 38,
            .Margin = Padding.Empty
        }
        _lastPageBtn.SetPrimary()

        _nextPageBtn = New BaseButton With {
            .Text = "›",
            .Width = 38,
            .Height = 38,
            .Margin = New Padding(6, 0, 0, 0)
        }
        _nextPageBtn.SetPrimary()

        _prevPageBtn = New BaseButton With {
            .Text = "‹",
            .Width = 38,
            .Height = 38,
            .Margin = New Padding(6, 0, 0, 0)
        }
        _prevPageBtn.SetPrimary()

        _firstPageBtn = New BaseButton With {
            .Text = "«",
            .Width = 38,
            .Height = 38,
            .Margin = New Padding(6, 0, 0, 0)
        }
        _firstPageBtn.SetPrimary()

        ' RightToLeft insertion: visual order left→right = [«] [‹] [›] [»]
        rightPanel.Controls.AddRange({_lastPageBtn, _nextPageBtn, _prevPageBtn, _firstPageBtn})

        _paginationLayout.Controls.Add(leftPanel, 0, 0)
        _paginationLayout.Controls.Add(_pageInfoLabel, 1, 0)
        _paginationLayout.Controls.Add(rightPanel, 2, 0)
        _paginationPanel.Controls.Add(_paginationLayout)

        ' 
        '  DGV
        ' 
        With _dgv
            .Dock = DockStyle.Fill
            .BackgroundColor = Colors.Background
            .BorderStyle = BorderStyle.None
            .AllowUserToAddRows = False
            .AllowUserToDeleteRows = False
            .ReadOnly = True
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            .MultiSelect = False
            .RowHeadersVisible = False
            .EnableHeadersVisualStyles = False
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            .RowTemplate.Height = 40

            .ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle With {
                .BackColor = Colors.Secondary,
                .ForeColor = Colors.OnSecondary,
                .Font = New Font(Strings.FONT_FAMILY, Dimen.LABEL_SM, FontStyle.Bold),
                .Padding = New Padding(8, 0, 0, 0)
            }
            .DefaultCellStyle = New DataGridViewCellStyle With {
                .BackColor = Colors.Background,
                .ForeColor = Colors.LblPrimary,
                .Font = New Font(Strings.FONT_FAMILY, Dimen.LABEL_SM),
                .SelectionBackColor = Colors.Accent,
                .SelectionForeColor = Color.White,
                .Padding = New Padding(8, 0, 0, 0)
            }
        End With

        _noResultsLabel = New Label With {
        .Text = "No results found",
        .Font = New Font(Strings.FONT_FAMILY, 11, FontStyle.Regular),
        .ForeColor = Colors.LblPrimary,
        .BackColor = Colors.Background,
        .TextAlign = ContentAlignment.MiddleCenter,
        .Dock = DockStyle.Fill,
        .Visible = False
}
        _dgv.Controls.Add(_noResultsLabel)
        _noResultsLabel.BringToFront()

        ' Dock order: Bottom → Top → Fill (Fill must be last)
        Me.Controls.Add(_dgv)
        Me.Controls.Add(_paginationPanel)
        Me.Controls.Add(_toolbarPanel)
    End Sub

    ' Event Wiring 
    Private Sub SetupEventHandlers()
        ' DGV
        AddHandler _dgv.CellClick, AddressOf OnDgvCellClick
        AddHandler _dgv.DataBindingComplete, AddressOf OnDataBindingComplete

        ' Search button click and Enter key
        AddHandler _searchBtn.Click, AddressOf OnSearchClicked
        AddHandler _searchInput.KeyDown, AddressOf OnSearchKeyDown

        ' Pagination nav
        AddHandler _firstPageBtn.Click, Sub(s, e) GoToPage(1)
        AddHandler _prevPageBtn.Click, Sub(s, e) GoToPage(_currentPage - 1)
        AddHandler _nextPageBtn.Click, Sub(s, e) GoToPage(_currentPage + 1)
        AddHandler _lastPageBtn.Click, Sub(s, e) GoToPage(TotalPages)

        ' Page size input numeric 
        AddHandler _pageSizeInput.KeyPress, AddressOf OnPageSizeKeyPress
        AddHandler _pageSizeInput.KeyDown, AddressOf OnPageSizeKeyDown
        AddHandler _pageSizeInput.Leave, AddressOf OnPageSizeLeave
    End Sub

    '  DGV Handlers
    Private Sub OnDgvCellClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 Then Return
        RefreshButtonVisibility()
    End Sub

    Private Sub OnDataBindingComplete(sender As Object, e As DataGridViewBindingCompleteEventArgs)
        If _isPaging OrElse _isFiltering Then Return
        _dgv.ClearSelection()
        RefreshButtonVisibility()
    End Sub

    Private Sub RefreshButtonVisibility()
        Dim hasSelection As Boolean = _dgv.SelectedRows.Count > 0
        For Each entry In _actionButtons
            Select Case entry.Visibility
                Case ButtonVisibility.Always
                    entry.Button.Visible = True
                Case ButtonVisibility.OnSelection
                    entry.Button.Visible = hasSelection
                Case ButtonVisibility.OnNoSelection
                    entry.Button.Visible = Not hasSelection
            End Select
        Next
    End Sub

    Public Sub AddActionButton(btn As BaseButton,
                           visibility As ButtonVisibility,
                           Optional insertAt As Integer = -1)
        _actionButtons.Add(New ActionButtonEntry With {
        .Button = btn,
        .Visibility = visibility
    })

        If btn.Margin = Padding.Empty Then
            btn.Margin = New Padding(6, 0, 0, 0)
        End If

        If insertAt >= 0 AndAlso insertAt < _actionButtonsPanel.Controls.Count Then
            _actionButtonsPanel.Controls.Add(btn)
            _actionButtonsPanel.Controls.SetChildIndex(btn, insertAt)
        Else
            _actionButtonsPanel.Controls.Add(btn)
        End If

        RefreshButtonVisibility()
    End Sub

    '  Search Handlers
    Private Sub OnSearchClicked(sender As Object, e As EventArgs)
        PerformSearch(_searchInput.Text)
    End Sub

    Private Sub OnSearchKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            _searchBtn.PerformClick()
            e.SuppressKeyPress = True
        End If
    End Sub

    '  Page Size Handlers 
    Private Sub OnPageSizeKeyPress(sender As Object, e As KeyPressEventArgs)
        If Not Char.IsDigit(e.KeyChar) AndAlso e.KeyChar <> ControlChars.Back Then
            e.Handled = True
        End If
    End Sub

    Private Sub OnPageSizeKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            ApplyPageSizeInput()
            e.SuppressKeyPress = True
        End If
    End Sub

    Private Sub OnPageSizeLeave(sender As Object, e As EventArgs)
        ApplyPageSizeInput()
    End Sub

    Private Sub ApplyPageSizeInput()
        Dim value As Integer
        If Integer.TryParse(_pageSizeInput.Text, value) AndAlso value > 0 Then
            _pageSize = value
        Else
            _pageSize = 10
            _pageSizeInput.Text = "10"
        End If
        _currentPage = 1
        ApplyPage()
    End Sub

    '  Data Binding 
    Public Sub BindDataSource(dt As DataTable)
        _originalDataTable = dt.Copy()
        _filteredDataTable = dt.Copy()
        _currentPage = 1
        ApplyPage()
    End Sub

    '  Search 
    Private Sub PerformSearch(query As String)
        If _originalDataTable Is Nothing Then Return

        If String.IsNullOrWhiteSpace(query) Then
            _filteredDataTable = _originalDataTable
        Else
            Dim filtered As DataTable = _originalDataTable.Clone()
            For Each row As DataRow In _originalDataTable.Rows
                For Each cell As Object In row.ItemArray
                    If cell IsNot Nothing AndAlso
                       cell.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 Then
                        filtered.ImportRow(row)
                        Exit For
                    End If
                Next
            Next
            _filteredDataTable = filtered
        End If

        _currentPage = 1
        ApplyPage()
    End Sub

    '  Pagination
    Private Sub GoToPage(page As Integer)
        Dim clamped As Integer = Math.Max(1, Math.Min(page, TotalPages))
        If clamped = _currentPage Then Return
        _currentPage = clamped
        ApplyPage()
    End Sub


    Private Sub ApplyPage()
        If _filteredDataTable Is Nothing Then Return

        Dim totalRows As Integer = _filteredDataTable.Rows.Count
        Dim totalPgs As Integer = TotalPages

        _currentPage = Math.Max(1, Math.Min(_currentPage, totalPgs))

        Dim startIndex As Integer = (_currentPage - 1) * _pageSize
        Dim takeCount As Integer = Math.Min(_pageSize, totalRows - startIndex)

        Dim pagedTable As DataTable = _filteredDataTable.Clone()

        If totalRows > 0 AndAlso startIndex >= 0 AndAlso startIndex < totalRows Then
            For i As Integer = startIndex To (startIndex + takeCount - 1)
                pagedTable.ImportRow(_filteredDataTable.Rows(i))
            Next
        End If

        _isPaging = True

        _dgv.SuspendLayout()
        _dgv.DataSource = Nothing
        _dgv.Columns.Clear()
        _dgv.AutoGenerateColumns = True
        _dgv.DataSource = pagedTable
        _dgv.ClearSelection()
        _dgv.ResumeLayout()

        _isPaging = False
        RefreshButtonVisibility()

        _pageInfoLabel.Text = $"Page {_currentPage} of {totalPgs}  ({totalRows} items)"

        _firstPageBtn.Enabled = _currentPage > 1
        _prevPageBtn.Enabled = _currentPage > 1
        _nextPageBtn.Enabled = _currentPage < totalPgs
        _lastPageBtn.Enabled = _currentPage < totalPgs

        _noResultsLabel.Visible = (totalRows = 0)
        If _noResultsLabel.Visible Then _noResultsLabel.BringToFront()

        RaiseEvent AfterPageApplied(Me, EventArgs.Empty)
    End Sub

    '  Public Helpers
    Public Function GetSelectedItem() As Dictionary(Of String, Object)
        If _dgv.SelectedRows.Count = 0 Then Return Nothing
        Dim row As DataGridViewRow = _dgv.SelectedRows(0)
        Dim result As New Dictionary(Of String, Object)
        For Each col As DataGridViewColumn In _dgv.Columns
            result(col.Name) = row.Cells(col.Name).Value
        Next
        Return result
    End Function

    Public Function GetSelectedRow() As DataGridViewRow
        If _dgv.SelectedRows.Count > 0 Then Return _dgv.SelectedRows(0)
        Return Nothing
    End Function

    Public Function GetSelectedValue(columnName As String) As Object
        Dim row As DataGridViewRow = GetSelectedRow()
        If row Is Nothing Then Return Nothing
        If Not _dgv.Columns.Contains(columnName) Then Return Nothing
        Return row.Cells(columnName).Value
    End Function

    Public Function GetSelectedRowIndex() As Integer
        If _dgv.SelectedRows.Count > 0 Then Return _dgv.SelectedRows(0).Index
        Return -1
    End Function

    Public Sub ResetSelection()
        _dgv.ClearSelection()
        RefreshButtonVisibility()
    End Sub

    Public Sub ClearSelection()
        ResetSelection()
    End Sub

    Public Function GetSearchText() As String
        Return _searchInput.Text
    End Function

    Public Sub SetPageSize(size As Integer)
        If size < 1 Then Return
        _pageSize = size
        _pageSizeInput.Text = size.ToString()
        _currentPage = 1
        ApplyPage()
    End Sub

    Public Sub FilterData(searchText As String, searchParam As String)
        Try
            If _dgv.DataSource Is Nothing Then Exit Sub

            Dim dt As DataTable = DirectCast(_dgv.DataSource, DataTable)

            If String.IsNullOrEmpty(searchText) Then
                dt.DefaultView.RowFilter = ""
            Else
                _isFiltering = True
                ' param sample: "Name LIKE '%{0}%' OR Email LIKE '%{0}%'"
                dt.DefaultView.RowFilter = String.Format(searchParam, searchText)
                If _dgv.Rows.Count > 0 Then
                    _dgv.Rows(0).Selected = True
                    RefreshButtonVisibility()
                End If

                _isFiltering = False  

            End If
        Catch ex As Exception
            Dim dlg = New BaseDialog()

            DialogTypes.Apply(dlg,
                DialogType.Error,
                "Unable to Search this Value",
                ex.Message)

            dlg.ShowDialog()
        End Try

    End Sub

End Class