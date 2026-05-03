Imports MySql.Data.MySqlClient

Public Class PricingForm
    Inherits BasePanel

    Private _subContainer As PrimaryFlowLayoutPanel

    Private _rateNameInput As BaseInputPanel
    Private _rateNameField As ValidationPanel

    Private _descInput As BaseInputPanel
    Private _descField As ValidationPanel

    Private _baseFeeInput As BaseNumericPanel
    Private _baseFeeField As ValidationPanel

    Private _dailyFeeInput As BaseNumericPanel
    Private _dailyFeeField As ValidationPanel

    Private _addButton As BaseButton
    Private _cancelButton As BaseButton

    Private _buttonTable As TableLayoutPanel
    Private _id As Integer?

    Public Sub New(Optional id As Integer? = Nothing)
        Me.Dock = DockStyle.Fill
        _id = id
        InitializeComponent()
        AddHandler Me.Resize, AddressOf CenterSubContainer
        AddHandler _subContainer.SizeChanged, AddressOf CenterSubContainer
        LoadAsync()
    End Sub

    Public Sub InitializeComponent()
        _subContainer = New PrimaryFlowLayoutPanel() With {
            .AutoSize = True,
            .FlowDirection = FlowDirection.TopDown,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .BackColor = Color.White,
            .Padding = New Padding(16),
            .BorderStyle = BorderStyle.FixedSingle
        }

        ' Rate Name
        _rateNameInput = New BaseInputPanel With {
            .LabelText = "Rate Name"
        }

        _rateNameField = New ValidationPanel(_rateNameInput)
        _rateNameField.SetValidator(New InputValidator().Required().MaxLength(20))

        ' Description

        _descInput = New BaseInputPanel With {
            .LabelText = "Short Description (optional)"
        }

        _descField = New ValidationPanel(_descInput)
        _descField.SetValidator(New InputValidator().MaxLength(40).NoSpecialChar())


        ' Base Fee

        _baseFeeInput = New BaseNumericPanel With {
            .LabelText = "Base / Starting Fee"
        }

        _baseFeeInput.SetPricingMode()

        _baseFeeField = New ValidationPanel(_baseFeeInput)
        _baseFeeField.SetValidator(New InputValidator().Required().MinValue(0).MaxValue(10000))

        ' Daily Fee

        _dailyFeeInput = New BaseNumericPanel With {
            .LabelText = "Daily Fee"
        }

        _dailyFeeInput.SetPricingMode()

        _dailyFeeField = New ValidationPanel(_dailyFeeInput)
        _dailyFeeField.SetValidator(New InputValidator().Required().MinValue(2).MaxValue(10000))


        ' Button Table
        _buttonTable = New TableLayoutPanel With {
             .ColumnCount = 2,
            .Padding = Padding.Empty,
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
             .Width = _subContainer.Width + 8,
            .Height = 40
        }

        _buttonTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        _buttonTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))

        _addButton = New BaseButton() With {
            .Text = "Add",
            .Dock = DockStyle.Top
        }

        _addButton.SetPrimary()

        _cancelButton = New BaseButton() With {
            .Text = "Cancel",
            .Dock = DockStyle.Top
        }

        _cancelButton.SetDanger()

        ' Controls

        _buttonTable.Controls.Add(_cancelButton, 0, 0)
        _buttonTable.Controls.Add(_addButton, 1, 0)

        With _subContainer.Controls
            .Add(_rateNameField)
            .Add(_descField)
            .Add(_baseFeeField)
            .Add(_dailyFeeField)
            .Add(_buttonTable)
        End With

        Me.Controls.Add(_subContainer)

        ' Bind Event

        AddHandler _addButton.Click, AddressOf QueryBuyer
        AddHandler _cancelButton.Click, AddressOf CancelAdd
    End Sub


    Private Sub CancelAdd()
        root.rootNav.GoBackPage()
    End Sub

    Private Async Sub QueryBuyer()

        If Not ValidateAllInputs() Then
            Exit Sub
        End If

        Dim confirm_dlg = New BaseDialog()

        Dim msg As String = "Are you sure you want to add this pricing?"

        If _id.HasValue() Then
            msg = "Are you sure you want to save changes to this pricing?"
        End If

        DialogTypes.Apply(confirm_dlg,
                 DialogType.Confirmation,
                 "Confirmation",
                 msg)

        If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
            Dim loadingDlg As New BaseDialog()

            Dim completed As Boolean = Await DialogTypes.ShowLoadingUntilAsync(
                loadingDlg,
                Form1.Instance,
                Async Function()

                    Dim queryResult As Boolean

                    If _id.HasValue() Then
                        queryResult = Await EditQuery()
                    Else
                        queryResult = Await AddQuery()
                    End If

                    If queryResult Then
                        Dim info_dlg = New BaseDialog()

                        DialogTypes.Apply(info_dlg,
                          DialogType.Info,
                          "Success",
                          "Changes was saved successfully")

                        Dim result_info_dlg = Await info_dlg.ShowBaseDialogAsync(Form1.Instance)

                        If result_info_dlg = DialogResultType.Confirm Then
                            root.rootNav.GoBackPage()
                        End If
                    Else
                        Dim error_dlg = New BaseDialog()

                        DialogTypes.Apply(error_dlg,
                          DialogType.Error,
                          "Error",
                          "Something went wrong")

                        error_dlg.ShowDialog()
                    End If

                End Function
            )
        Else
            confirm_dlg.Hide()
        End If

    End Sub

    Private Function ValidateAllInputs() As Boolean
        Return {_rateNameField, _descField, _baseFeeField, _dailyFeeField}.All(Function(f) f.ValidateInput())
    End Function

    Private Async Function AddQuery() As Task(Of Boolean)
        Dim sql As String =
        $"INSERT INTO {Pricing.table_name} " &
        $"({Pricing.rate_label}, {Pricing.description}, {Pricing.base_fee}, {Pricing.daily_increment_fee}) " &
        $"VALUES (@{Pricing.rate_label}, @{Pricing.description}, @{Pricing.base_fee}, @{Pricing.daily_increment_fee})"

        Dim params As New Dictionary(Of String, Object) From {
        {$"@{Pricing.rate_label}", _rateNameInput.Value.Trim()},
        {$"@{Pricing.description}", ToDbNull(_descInput.Value.Trim())},
        {$"@{Pricing.base_fee}", _baseFeeInput.Value},
        {$"@{Pricing.daily_increment_fee}", _dailyFeeInput.Value}
        }

        Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)

        If affectedRows > 0 Then
            Return True
        End If

        Return False
    End Function


    Private Async Sub LoadAsync()
        ' Await loadDataForCmb()
        If _id.HasValue() Then
            Await fetchDataForEditMode(_id.Value)
        End If
    End Sub

    Private Async Function fetchDataForEditMode(id As Integer) As Task
        _addButton.Text = "Save"

        _baseFeeInput.Enabled = False
        _dailyFeeInput.Enabled = False

        Dim sql As String =
        $"SELECT {Pricing.rate_label}, {Pricing.description}, {Pricing.base_fee}, {Pricing.daily_increment_fee} " &
        $"FROM {Pricing.table_name} " &
        $"WHERE {Pricing.id} = @{Pricing.id}"

        Dim params As New Dictionary(Of String, Object) From {
            {$"@{Pricing.id}", id}
        }

        Dim reader As MySqlDataReader = Await ReadQueryAsync(sql, params)

        If reader IsNot Nothing Then
            While Await reader.ReadAsync()
                Dim rateName As String = reader(Pricing.rate_label)
                Dim desc As String = If(IsDBNull(reader(Pricing.description)), "", reader(Pricing.description))
                Dim baseFee = reader(Pricing.base_fee)
                Dim dailyfee = reader(Pricing.daily_increment_fee)

                _rateNameInput.SetValue(rateName)
                _descInput.SetValue(desc)
                _baseFeeInput.SetValue(baseFee)
                _dailyFeeInput.SetValue(dailyfee)
            End While

            reader.Close()
        End If
    End Function

    Private Async Function EditQuery() As Task(Of Boolean)
        Dim sql As String =
       $"UPDATE {Pricing.table_name} SET " &
       $"{Pricing.description} = @{Pricing.description}, " &
       $"{Pricing.base_fee} = @{Pricing.base_fee}, " &
       $"{Pricing.daily_increment_fee} = @{Pricing.daily_increment_fee} " &
       $"WHERE {Pricing.id} = @{Pricing.id}"

        Dim params As New Dictionary(Of String, Object) From {
        {$"@{Pricing.id}", _id},
        {$"@{Pricing.rate_label}", _rateNameInput.Value.Trim()},
        {$"@{Pricing.description}", ToDbNull(_descInput.Value.Trim())},
        {$"@{Pricing.base_fee}", _baseFeeInput.Value},
        {$"@{Pricing.daily_increment_fee}", _dailyFeeInput.Value}
        }

        Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)
        If affectedRows > 0 Then
            Return True
        End If
        Return False
    End Function


    Private Sub CenterSubContainer(sender As Object, e As EventArgs)
        _subContainer.Left = (Me.ClientSize.Width - _subContainer.Width) \ 2
        _subContainer.Top = (Me.ClientSize.Height - _subContainer.Height) \ 2
    End Sub
End Class
