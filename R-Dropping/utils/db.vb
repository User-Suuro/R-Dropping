
Imports MySql.Data.MySqlClient
Module Db
    Public myadocon, conn As New MySqlConnection
    Public cmd As New MySqlCommand
    Public cmdRead As MySqlDataReader

    Public db_server As String
    Public db_uid As String
    Public db_pwd As String
    Public db_name As String
    Public db_port As String
    Public strConnection As String


    Public Sub UpdateConnectionString(server As String,
                                      port As String,
                                      uid As String,
                                      password As String,
                                      database As String)

        db_server = server
        db_port = port
        db_uid = uid
        db_pwd = password
        db_name = database

        strConnection =
            "server=" & db_server & ";" &
            "port=" & db_port & ";" &
            "uid=" & db_uid & ";" &
            "password=" & db_pwd & ";" &
            "database=" & db_name & ";" &
            "Allow User Variables=True;"

    End Sub

    Public Async Function OpenConnAsync() As Task(Of Boolean)

        Try
            If conn Is Nothing Then
                conn = New MySqlConnection()
            End If

            If conn.State = ConnectionState.Open Then
                Await conn.CloseAsync()
            End If

            conn.ConnectionString = strConnection
            Await conn.OpenAsync()

            Return True

        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical)
            Return False
        End Try

    End Function

    Public Async Function ReadQueryAsync(sql As String) As Task(Of MySqlDataReader)
        Try
            Dim opened As Boolean = Await OpenConnAsync()

            If opened = False Then
                Return Nothing
            End If

            cmd = New MySqlCommand(sql, conn)

            cmdRead = CType(Await cmd.ExecuteReaderAsync(), MySqlDataReader)

            Return cmdRead

        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical)
            Return Nothing
        End Try
    End Function

    Public Async Function ReadQueryAsync(sql As String, params As Dictionary(Of String, Object)) As Task(Of MySqlDataReader)

        Try
            If Not Await OpenConnAsync() Then
                Return Nothing
            End If

            cmd = New MySqlCommand(sql, conn)

            For Each param In params
                cmd.Parameters.AddWithValue(param.Key, param.Value)
            Next

            Return Await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection)

        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical)
            Return Nothing
        End Try

    End Function



    Public Async Function ExecuteQueryAsync(sql As String, params As Dictionary(Of String, Object)) As Task(Of Integer)

        Try
            If Not Await OpenConnAsync() Then
                Return 0
            End If

            cmd = New MySqlCommand(sql, conn)

            For Each param In params
                cmd.Parameters.AddWithValue(param.Key, param.Value)
            Next

            Return Await cmd.ExecuteNonQueryAsync()

        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical)
            Return 0
        End Try

    End Function

    Public Async Function IsConnectedAsync() As Task(Of Boolean)

        Try
            Using testConn As New MySqlConnection(strConnection)

                Await testConn.OpenAsync()

                If testConn.State = ConnectionState.Open Then
                    Await testConn.CloseAsync()
                    Return True
                End If

            End Using

            Return False

        Catch ex As Exception
            Return False
        End Try

    End Function



    Public Function ToDbNull(value As String) As Object
        Return If(String.IsNullOrWhiteSpace(value), DBNull.Value, value)
    End Function

End Module