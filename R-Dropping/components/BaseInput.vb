Imports Guna.UI2.WinForms

Public Class BaseInputPanel
    Inherits PrimaryPanel
    Implements IValueProvider
    Implements IValidationStyleable

    Public Event ValueChanged As EventHandler Implements IValueProvider.ValueChanged

    Private _lblTitle As BaseLabel
    Private _txtInput As Guna2TextBox

    Private Const PanelHeight As Integer = 56

    Public Sub New()
        MyBase.New()
        Me.DoubleBuffered = True
        Me.Height = PanelHeight
        InitializeComponents()
        AttachEvents()
    End Sub


    Private Sub InitializeComponents()
        _lblTitle = New BaseLabel()
        With _lblTitle
            .SetSmall()
            .Dock = DockStyle.Top
            .Padding = New Padding(0, 0, 0, 4)
        End With

        _txtInput = New Guna2TextBox()
        With _txtInput
            .Dock = DockStyle.Top
            .BorderRadius = 4
            .PlaceholderForeColor = Colors.LblMuted
            .Height = 28
            .Font = New Font("Segoe UI", 9.0F)
            .ForeColor = Color.Black
        End With

        Me.Controls.Add(_txtInput)
        Me.Controls.Add(_lblTitle)
    End Sub

    Private Sub AttachEvents()
        AddHandler _txtInput.TextChanged,
            Sub(s As Object, e As EventArgs)
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Sub
    End Sub

    Public Sub SetValue(value As String)
        _txtInput.Text = value
    End Sub


    Public ReadOnly Property Value As String Implements IValueProvider.Value
        Get
            Return _txtInput.Text
        End Get
    End Property

    ' Validation Style

    Public Sub OnValidationError() Implements IValidationStyleable.OnValidationError
        _txtInput.BorderColor = Color.Red
    End Sub

    Public Sub OnValidationClear() Implements IValidationStyleable.OnValidationClear
        _txtInput.BorderColor = Color.FromArgb(213, 218, 223)
    End Sub


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
            Return _txtInput.PlaceholderText
        End Get
        Set(value As String)
            _txtInput.PlaceholderText = value
        End Set
    End Property


    Public ReadOnly Property InputControl As Guna2TextBox
        Get
            Return _txtInput
        End Get
    End Property

End Class


Public Class BaseNumericPanel
    Inherits PrimaryPanel
    Implements IValueProvider
    Implements IValidationStyleable

    Public Event ValueChanged As EventHandler Implements IValueProvider.ValueChanged

    Private _lblTitle As BaseLabel
    Private _numInput As Guna2NumericUpDown

    Private Const PanelHeight As Integer = 56

    Public Sub New()
        MyBase.New()
        Me.DoubleBuffered = True
        Me.Height = PanelHeight
        InitializeComponents()
        AttachEvents()
    End Sub

    Private Sub InitializeComponents()
        _lblTitle = New BaseLabel()
        With _lblTitle
            .SetSmall()
            .Dock = DockStyle.Top
            .Padding = New Padding(0, 0, 0, 4)
        End With

        _numInput = New Guna2NumericUpDown()
        With _numInput
            .Dock = DockStyle.Top
            .BorderRadius = 4
            .Height = 28
            .Font = New Font("Segoe UI", 9.0F)
            .ForeColor = Color.Black
            .Minimum = Decimal.MinValue
            .Maximum = Decimal.MaxValue
            .DecimalPlaces = 0
            .Value = 0
        End With

        Me.Controls.Add(_numInput)
        Me.Controls.Add(_lblTitle)
    End Sub

    Private Sub AttachEvents()
        AddHandler _numInput.ValueChanged,
            Sub(s As Object, e As EventArgs)
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End Sub
    End Sub

    ' Value

    Public Sub SetValue(value As Decimal)
        _numInput.Value = Math.Max(_numInput.Minimum, Math.Min(_numInput.Maximum, value))
    End Sub

    Public ReadOnly Property Value As String Implements IValueProvider.Value
        Get
            Return _numInput.Value.ToString()
        End Get
    End Property

    Public ReadOnly Property NumericValue As Decimal
        Get
            Return _numInput.Value
        End Get
    End Property

    ' Validation Style

    Public Sub OnValidationError() Implements IValidationStyleable.OnValidationError
        _numInput.BorderColor = Color.Red
    End Sub

    Public Sub OnValidationClear() Implements IValidationStyleable.OnValidationClear
        _numInput.BorderColor = Color.FromArgb(213, 218, 223)
    End Sub

    ' Public Acccess

    Public Property LabelText As String
        Get
            Return _lblTitle.Text
        End Get
        Set(value As String)
            _lblTitle.Text = value
        End Set
    End Property

    Public Property Minimum As Decimal
        Get
            Return _numInput.Minimum
        End Get
        Set(value As Decimal)
            _numInput.Minimum = value
        End Set
    End Property

    Public Property Maximum As Decimal
        Get
            Return _numInput.Maximum
        End Get
        Set(value As Decimal)
            _numInput.Maximum = value
        End Set
    End Property

    Public Property DecimalPlaces As Integer
        Get
            Return _numInput.DecimalPlaces
        End Get
        Set(value As Integer)
            _numInput.DecimalPlaces = value
        End Set
    End Property

    Public Property Increment As Decimal
        Get
            Return _numInput.Increment
        End Get
        Set(value As Decimal)
            _numInput.Increment = value
        End Set
    End Property

    Public Sub SetPricingMode()
        _numInput.Minimum = 0
        _numInput.Maximum = Decimal.MaxValue
        _numInput.DecimalPlaces = 2
        _numInput.Increment = 2D
        _numInput.Value = 0.00D
    End Sub

    Public ReadOnly Property InputControl As Guna2NumericUpDown
        Get
            Return _numInput
        End Get
    End Property

End Class


