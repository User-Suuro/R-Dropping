Imports System.IO
Imports MySql.Data.MySqlClient

Public Class BackupRestorePage
    Inherits BasePanel

    Private routeName As String = "Backup And Restore"

    Private _backupSqlBtn As BaseButton
    Private _backupCsvBtn As BaseButton

    Private _browseBtn As BaseButton
    Private _restoreBtn As BaseButton
    Private _filePathInput As BaseInputPanel
    Private _selectedFile As String = String.Empty

    Private ReadOnly _tables As String() = {
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

    Public Sub New()
        Me.Dock = DockStyle.Fill
        InitializeComponent()
        SetupEventHandlers()
        root.RootInstance.SetRouteLabel(routeName)
    End Sub

    Private Sub InitializeComponent()

        Dim wrapper As New Panel With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(24)
        }

        Dim layout As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2
        }
        layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 160))
        layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 160))
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

        layout.Controls.Add(BuildBackupCard(), 0, 0)
        layout.Controls.Add(BuildRestoreCard(), 0, 1)

        wrapper.Controls.Add(layout)
        Me.Controls.Add(wrapper)
    End Sub

    Private Function BuildBackupCard() As Panel
        Dim card As New Panel With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(16),
            .Margin = New Padding(0, 0, 0, 12),
            .BackColor = Color.FromArgb(245, 245, 245)
        }

        card.Controls.Add(New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 1,
            .BackColor = Color.LightGray
        })

        Dim title As New Label With {
            .Text = "BACKUP",
            .Dock = DockStyle.Top,
            .Height = 24,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .ForeColor = Color.FromArgb(60, 60, 60)
        }

        Dim desc As New Label With {
            .Text = "Export all table data to a file you can store safely.",
            .Dock = DockStyle.Top,
            .Height = 22,
            .Font = New Font("Segoe UI", 8.5F),
            .ForeColor = Color.Gray
        }

        Dim btnRow As New FlowLayoutPanel With {
            .Dock = DockStyle.Bottom,
            .Height = 46,
            .FlowDirection = FlowDirection.LeftToRight,
            .Padding = New Padding(0, 4, 0, 0)
        }

        _backupSqlBtn = New BaseButton With {
            .Text = "Backup as .SQL",
            .Width = 140,
            .Height = 38,
            .Margin = New Padding(0, 0, 8, 0)
        }
        _backupSqlBtn.SetPrimary()

        _backupCsvBtn = New BaseButton With {
            .Text = "Backup as .CSV",
            .Width = 140,
            .Height = 38,
            .Margin = Padding.Empty
        }
        _backupCsvBtn.SetPrimary()

        btnRow.Controls.Add(_backupSqlBtn)
        btnRow.Controls.Add(_backupCsvBtn)

        card.Controls.Add(btnRow)
        card.Controls.Add(desc)
        card.Controls.Add(title)

        Return card
    End Function

    Private Function BuildRestoreCard() As Panel
        Dim card As New Panel With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(16),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle
        }

        card.Controls.Add(New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 1,
            .BackColor = Color.LightGray
        })



        Dim bottomPanel As New FlowLayoutPanel With {
            .Dock = DockStyle.Top,
            .Height = 72,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .Padding = New Padding(0, 4, 0, 0)
        }

        _filePathInput = New BaseInputPanel With {
            .PlaceholderText = "No file selected...'",
            .LabelText = "Restore"
        }

        _filePathInput.InputControl.ReadOnly = True

        _browseBtn = New BaseButton With {
        .Text = "Browse",
        .Width = 110,
        .Height = 52,
        .Margin = New Padding(8, 0, 8, 0)
        }
        _browseBtn.SetPrimary()

        _restoreBtn = New BaseButton With {
        .Text = "Restore",
        .Width = 110,
        .Height = 52,
        .Margin = Padding.Empty,
        .Enabled = False
        }
        _restoreBtn.SetDanger()

        AddHandler bottomPanel.Resize, Sub(s, e)
                                           _filePathInput.Width = bottomPanel.ClientSize.Width -
                               (_browseBtn.Width + _browseBtn.Margin.Horizontal) -
                               (_restoreBtn.Width + _restoreBtn.Margin.Horizontal) - 4
                                       End Sub

        bottomPanel.Controls.Add(_filePathInput)
        bottomPanel.Controls.Add(_browseBtn)
        bottomPanel.Controls.Add(_restoreBtn)

        card.Controls.Add(bottomPanel)

        Return card
    End Function

    Private Sub SetupEventHandlers()


        AddHandler _backupSqlBtn.Click, Async Sub(s, e)
                                            Await ConfirmThenRun(
                                                "Generate a .SQL backup of all tables?",
                                                AddressOf BackupAsSqlAsync)
                                        End Sub

        AddHandler _backupCsvBtn.Click, Async Sub(s, e)
                                            Await ConfirmThenRun(
                                                "Generate a .CSV backup of all tables?",
                                                AddressOf BackupAsCsvAsync)
                                        End Sub

        AddHandler _browseBtn.Click, Sub(s, e)
                                         Using dlg As New OpenFileDialog With {
                                             .Title = "Select SQL backup file",
                                             .Filter = "SQL Files (*.sql)|*.sql"
                                         }
                                             If dlg.ShowDialog() = DialogResult.OK Then
                                                 _selectedFile = dlg.FileName
                                                 _filePathInput.SetValue(_selectedFile)
                                                 _restoreBtn.Enabled = True
                                             End If
                                         End Using
                                     End Sub

        AddHandler _restoreBtn.Click, Async Sub(s, e)
                                          Await ConfirmThenRun(
                                              "This will overwrite existing data. Are you sure you want to restore?",
                                              AddressOf RestoreFromSqlAsync)
                                      End Sub
    End Sub

    Private Async Function ConfirmThenRun(message As String, action As Func(Of Task)) As Task
        Dim confirm_dlg = New BaseDialog()
        DialogTypes.Apply(confirm_dlg, DialogType.Confirmation, "Confirmation", message)

        If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
            Dim loadingDlg As New BaseDialog()
            Await DialogTypes.ShowLoadingUntilAsync(loadingDlg, Form1.Instance, action)
        End If
    End Function


    Private Async Function BackupAsSqlAsync() As Task
        Try
            Dim backupDir As String = Config.FindSolutionRoot("backup")
            If backupDir Is Nothing Then Throw New Exception("Could not locate solution root.")

            Dim fileName As String = $"sql_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql"
            Dim fullPath As String = Path.Combine(backupDir, fileName)

            Using writer As New StreamWriter(fullPath, False, System.Text.Encoding.UTF8)
                Await writer.WriteLineAsync($"-- Backup generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                Await writer.WriteLineAsync($"-- Database: r-dropping")
                Await writer.WriteLineAsync()

                For Each tableName In _tables
                    Await writer.WriteLineAsync($"-- Table: {tableName}")
                    Await writer.WriteLineAsync($"DELETE FROM `{tableName}`;")

                    ' Read all rows into memory while the reader is alive
                    Dim rows As New List(Of String())
                    Using reader = Await ReadQueryAsync($"SELECT * FROM `{tableName}`")
                        If reader IsNot Nothing Then
                            While Await reader.ReadAsync()
                                Dim fields(reader.FieldCount - 1) As String
                                For i As Integer = 0 To reader.FieldCount - 1
                                    If reader.IsDBNull(i) Then
                                        fields(i) = "NULL"
                                    Else
                                        fields(i) = $"'{reader.GetValue(i).ToString().Replace("'", "''")}'"
                                    End If
                                Next
                                rows.Add(fields)
                            End While
                        End If
                    End Using

                    ' Write the collected rows
                    For Each fields In rows
                        Await writer.WriteLineAsync($"INSERT INTO `{tableName}` VALUES ({String.Join(", ", fields)});")
                    Next

                    Await writer.WriteLineAsync()
                Next
            End Using

            OpenFolder(Path.GetDirectoryName(fullPath))

        Catch ex As Exception
            ShowError(ex.Message)
        End Try
    End Function

    Private Async Function BackupAsCsvAsync() As Task
        Try
            Dim backupDir As String = Config.FindSolutionRoot("backup")
            If backupDir Is Nothing Then Throw New Exception("Could not locate solution root.")

            Dim folderName As String = $"csv_{DateTime.Now:yyyyMMdd_HHmmss}"
            Dim folderPath As String = Path.Combine(backupDir, folderName)
            Directory.CreateDirectory(folderPath)

            For Each tableName In _tables
                Dim csvFile As String = Path.Combine(folderPath, $"{tableName}.csv")
                Using writer As New StreamWriter(csvFile, False, System.Text.Encoding.UTF8)
                    ' Read header and all rows
                    Dim colNames As List(Of String) = Nothing
                    Dim rows As New List(Of String())

                    Using reader = Await ReadQueryAsync($"SELECT * FROM `{tableName}`")
                        If reader IsNot Nothing Then
                            ' Get column names once
                            If colNames Is Nothing Then
                                colNames = New List(Of String)
                                For i As Integer = 0 To reader.FieldCount - 1
                                    colNames.Add(reader.GetName(i))
                                Next
                                writer.WriteLine(String.Join(",", colNames))  ' Write header
                            End If

                            While Await reader.ReadAsync()
                                Dim fields(reader.FieldCount - 1) As String
                                For i As Integer = 0 To reader.FieldCount - 1
                                    If reader.IsDBNull(i) Then
                                        fields(i) = ""
                                    Else
                                        Dim v As String = reader.GetValue(i).ToString()
                                        If v.Contains(",") OrElse v.Contains("""") OrElse v.Contains(vbLf) Then
                                            v = """" & v.Replace("""", """""") & """"
                                        End If
                                        fields(i) = v
                                    End If
                                Next
                                rows.Add(fields)
                            End While
                        End If
                    End Using


                    For Each fields In rows
                        writer.WriteLine(String.Join(",", fields))
                    Next
                End Using
            Next

            OpenFolder(folderPath)

        Catch ex As Exception
            ShowError(ex.Message)
        End Try
    End Function

    Private Async Function RestoreFromSqlAsync() As Task
        Try
            Dim sqlContent As String = Await Task.Run(Function() File.ReadAllText(_selectedFile))

            Using conn As New MySqlConnection(Db.strConnection)
                Await conn.OpenAsync()


                Using cmd As New MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", conn)
                    Await cmd.ExecuteNonQueryAsync()
                End Using

                For Each tableName In _tables
                    Using cmd As New MySqlCommand($"DROP TABLE IF EXISTS `{tableName}`;", conn)
                        Await cmd.ExecuteNonQueryAsync()
                    End Using
                Next


                Using cmd As New MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", conn)
                    Await cmd.ExecuteNonQueryAsync()
                End Using


                Dim script As New MySqlScript(conn, sqlContent)
                Await script.ExecuteAsync()
            End Using

            _selectedFile = String.Empty
            _filePathInput.Text = String.Empty
            _restoreBtn.Enabled = False

            Dim info_dlg = New BaseDialog()
            DialogTypes.Apply(info_dlg, DialogType.Info, "Success", "Database restored successfully.")
            Await info_dlg.ShowBaseDialogAsync(Form1.Instance)

        Catch ex As Exception
            ShowError(ex.Message)
        End Try
    End Function

    Private Sub OpenFolder(path As String)
        If Not Directory.Exists(path) Then
            Directory.CreateDirectory(path)
        End If
        Process.Start(New ProcessStartInfo(path) With {.UseShellExecute = True})
    End Sub

    Private Sub ShowError(message As String)
        MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub

End Class