Module Module1

    Sub Main()
        Dim machine As New Machine
        machine.DebugSetup()
        While True
            Console.Write(machine.Prompt & "$ ")
            Dim input As String = Console.ReadLine
            machine.Main(input)
            Console.WriteLine()
        End While
    End Sub

End Module
