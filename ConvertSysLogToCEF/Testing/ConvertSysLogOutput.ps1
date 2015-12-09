
function ConvertSysLogMessages($iReceivePort, $iSendPort)
{
  $oIPEndPoint = new-object System.Net.IPEndPoint ([system.net.ipaddress]::any, $iReceivePort)
  $oTCPListener = new-object System.Net.Sockets.TcpListener $oIPEndPoint
  $oTCPListener.start()

  do 
  {
    $oReceiveTCPClient = $oTCPListener.AcceptTcpClient()
    $oSendTCPClient = new-object System.Net.Sockets.TcpClient "localhost", $iSendPort
    $oReceiveStream = $oReceiveTCPClient.GetStream()
    $oSendStream = $oSendTCPClient.GetStream()
    $oStreamReader = new-object System.IO.StreamReader $oReceiveStream
    $oStreamWriter = new-object System.IO.StreamWriter $oSendStream

    do 
    {
      $sLine = $oStreamReader.ReadLine()
      if ($sLine)
      { 
        $sCEF = (ConvertToCEF "CEF:0" "Tanium" "TaniumApplicationServer" "6.5.314.4316" "0" "0" $sLine) + "`n"
        #Write-Host "Raw Answer:" $sLine
        #Write-Host "Converted Answer:" $sCEF
        $oStreamWriter.Write($sCEF)
      }
      
    } while ($sLine -and $sLine -ne ([char]4))

    $oStreamReader.Dispose()
    $oStreamWriter.Dispose()
    $oReceiveStream.Dispose()
    $oSendStream.Dispose()
    $oReceiveTCPClient.Dispose()
    $oSendTCPClient.Dispose()
  } while ($sLine -ne ([char]4))

  $oTCPListener.stop()
}

function ConvertToCEF
{
  param($sVersion, $sDeviceVendor, $sDeviceProduct, $sDeviceVersion, $sSignatureID, $sSeverity, $sSysLog)
  $aSysLog = $sSysLog.Split("[")
  $sText = ($aSysLog[1].Split("]"))[0]
  $aText = $sText.Split(" ")
  $sQuestion = $aText[0]
  $i = 0
  $iUpper = $aText.length - 1
  foreach ($sAnswer in $aText)
  {
    if  (($i -ne 0) -and ($i -ne $iUpper)) { $sAnswers += $sAnswer + " " }
    elseif ($i -eq $iUpper) { $sAnswers += $sAnswer }
    $i++
  }
  $sRet = $sVersion + "|" + $sDeviceVendor + "|" + $sDeviceProduct + "|" + $sDeviceVersion + "|" + $sSignatureID + "|" + $sQuestion + "|" + $sSeverity + "|" + $sAnswers

  return $sRet
}

ConvertSysLogMessages 17480 17481