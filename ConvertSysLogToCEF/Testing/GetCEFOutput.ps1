
function GetCEFOutput($iReceivePort)
{
  $oIPEndPoint = new-object System.Net.IPEndPoint ([system.net.ipaddress]::any, $iReceivePort)
  $oTCPListener = new-object System.Net.Sockets.TcpListener $oIPEndPoint
  $oTCPListener.start()

  do 
  {
    $oReceiveTCPClient = $oTCPListener.AcceptTcpClient()
    $oReceiveStream = $oReceiveTCPClient.GetStream()
    $oStreamReader = new-object System.IO.StreamReader $oReceiveStream

    do 
    {
      $sLine = $oStreamReader.ReadLine()
      if ($sLine) { Write-Host $sLine }
      
    } while ($sLine -and $sLine -ne ([char]4))

    $oStreamReader.Dispose()
    $oReceiveStream.Dispose()
    $oReceiveTCPClient.Dispose()
  } while ($sLine -ne ([char]4))

  $oTCPListener.stop()
}

GetCEFOutput 17481