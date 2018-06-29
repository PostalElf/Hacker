Module Module1

    Sub Main()
        Dim testMachine As New Machine
        testMachine.DebugSetupTestMachine()
        Player.AddUID(testMachine)

        Dim mainframe As New Machine
        mainframe.DebugSetupMainframe()
        Player.AddMount(mainframe)
        Dim drone As New Machine
        drone.DebugSetupDrone()
        Player.AddMount(drone)

        While True
            Dim am As Machine = Player.ActiveMount
            Console.Write(am.Prompt & "$ ")
            Dim input As String = Console.ReadLine
            am.Main(input)
            Console.WriteLine()
        End While
    End Sub

End Module
