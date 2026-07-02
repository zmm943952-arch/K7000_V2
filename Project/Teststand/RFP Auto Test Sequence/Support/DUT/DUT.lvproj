<?xml version='1.0' encoding='UTF-8'?>
<Project Type="Project" LVVersion="24008000">
	<Property Name="NI.LV.All.SaveVersion" Type="Str">24.0</Property>
	<Property Name="NI.LV.All.SourceOnly" Type="Bool">true</Property>
	<Item Name="我的电脑" Type="My Computer">
		<Property Name="server.app.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="server.control.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="server.tcp.enabled" Type="Bool">false</Property>
		<Property Name="server.tcp.port" Type="Int">0</Property>
		<Property Name="server.tcp.serviceName" Type="Str">我的电脑/VI服务器</Property>
		<Property Name="server.tcp.serviceName.default" Type="Str">我的电脑/VI服务器</Property>
		<Property Name="server.vi.callsEnabled" Type="Bool">true</Property>
		<Property Name="server.vi.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="specify.custom.address" Type="Bool">false</Property>
		<Item Name="Controls" Type="Folder" URL="../Controls">
			<Property Name="NI.DISK" Type="Bool">true</Property>
		</Item>
		<Item Name="dbc" Type="Folder" URL="../dbc">
			<Property Name="NI.DISK" Type="Bool">true</Property>
		</Item>
		<Item Name="dll" Type="Folder" URL="../dll">
			<Property Name="NI.DISK" Type="Bool">true</Property>
		</Item>
		<Item Name="KvCAN" Type="Folder" URL="../KvCAN">
			<Property Name="NI.DISK" Type="Bool">true</Property>
		</Item>
		<Item Name="SubVIs" Type="Folder" URL="../SubVIs">
			<Property Name="NI.DISK" Type="Bool">true</Property>
		</Item>
		<Item Name="3E80.vi" Type="VI" URL="../3E80.vi"/>
		<Item Name="CAN Status.vi" Type="VI" URL="../CAN Status.vi"/>
		<Item Name="DBC Op.vi" Type="VI" URL="../DBC Op.vi"/>
		<Item Name="EnterCalibrationMode.vi" Type="VI" URL="../EnterCalibrationMode.vi"/>
		<Item Name="HardwareVersion.vi" Type="VI" URL="../HardwareVersion.vi"/>
		<Item Name="HexFile.vi" Type="VI" URL="../HexFile.vi"/>
		<Item Name="MoveDone.vi" Type="VI" URL="../MoveDone.vi"/>
		<Item Name="ProductNumber.vi" Type="VI" URL="../ProductNumber.vi"/>
		<Item Name="RadarAngle.vi" Type="VI" URL="../RadarAngle.vi"/>
		<Item Name="RadarEnable.vi" Type="VI" URL="../RadarEnable.vi"/>
		<Item Name="ReadFrames v2.vi" Type="VI" URL="../ReadFrames v2.vi"/>
		<Item Name="ReadFrames.vi" Type="VI" URL="../ReadFrames.vi"/>
		<Item Name="ReadFramesSingle.vi" Type="VI" URL="../ReadFramesSingle.vi"/>
		<Item Name="ReadPoint.vi" Type="VI" URL="../ReadPoint.vi"/>
		<Item Name="ReadPointSingle.vi" Type="VI" URL="../ReadPointSingle.vi"/>
		<Item Name="RequestDownload.vi" Type="VI" URL="../RequestDownload.vi"/>
		<Item Name="RequestTransferExit.vi" Type="VI" URL="../RequestTransferExit.vi"/>
		<Item Name="ResetECU.vi" Type="VI" URL="../ResetECU.vi"/>
		<Item Name="RF CalibrationData.vi" Type="VI" URL="../RF CalibrationData.vi"/>
		<Item Name="RF CalibrationDataReWrite.vi" Type="VI" URL="../RF CalibrationDataReWrite.vi"/>
		<Item Name="RoutineControl.vi" Type="VI" URL="../RoutineControl.vi"/>
		<Item Name="SaveDataLog.vi" Type="VI" URL="../SaveDataLog.vi"/>
		<Item Name="SaveHexFile V1.4.vi" Type="VI" URL="../SaveHexFile V1.4.vi"/>
		<Item Name="SecurityAccess.vi" Type="VI" URL="../SecurityAccess.vi"/>
		<Item Name="SessionControl.vi" Type="VI" URL="../SessionControl.vi"/>
		<Item Name="SoftwareVersion.vi" Type="VI" URL="../SoftwareVersion.vi"/>
		<Item Name="TransferData.vi" Type="VI" URL="../TransferData.vi"/>
		<Item Name="weifu boot call.vi" Type="VI" URL="../weifu boot call.vi"/>
		<Item Name="weifu boot.vi" Type="VI" URL="../weifu boot.vi"/>
		<Item Name="依赖关系" Type="Dependencies">
			<Item Name="instr.lib" Type="Folder">
				<Item Name="kvCanBusOff.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanBusOff.vi"/>
				<Item Name="kvCanBusOn.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanBusOn.vi"/>
				<Item Name="kvCanClose.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanClose.vi"/>
				<Item Name="kvCanError.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanError.vi"/>
				<Item Name="kvCanInitialize.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanInitialize.vi"/>
				<Item Name="kvCanIoCtlFlush.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanIoCtlFlush.vi"/>
				<Item Name="kvCanOpenChannel.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanOpenChannel.vi"/>
				<Item Name="kvCanReadSpecific.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanReadSpecific.vi"/>
				<Item Name="kvCanReadWait.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanReadWait.vi"/>
				<Item Name="kvCanSetBusParams.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanSetBusParams.vi"/>
				<Item Name="kvCanSetBusParamsFd.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanSetBusParamsFd.vi"/>
				<Item Name="kvCanSetStdBusParams.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanSetStdBusParams.vi"/>
				<Item Name="kvCanSetStdBusParamsFd.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanSetStdBusParamsFd.vi"/>
				<Item Name="kvCanTranslateBaud.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanTranslateBaud.vi"/>
				<Item Name="kvCanUnloadLibrary.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanUnloadLibrary.vi"/>
				<Item Name="kvCanWrite.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanWrite.vi"/>
				<Item Name="kvCanWriteMsgFlag.vi" Type="VI" URL="/&lt;instrlib&gt;/kvCanVi/Kvaser CAN VI 20230117/22.0/kvCanVI.llb/kvCanWriteMsgFlag.vi"/>
			</Item>
			<Item Name="vi.lib" Type="Folder">
				<Item Name="_XNET Convert List From Array To Comma.vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/_XNET Convert List From Array To Comma.vi"/>
				<Item Name="_XNET Create Session.vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/_XNET Create Session.vi"/>
				<Item Name="_XNET Split Database Cluster.vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/_XNET Split Database Cluster.vi"/>
				<Item Name="Application Directory.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/file.llb/Application Directory.vi"/>
				<Item Name="Check if File or Folder Exists.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/libraryn.llb/Check if File or Folder Exists.vi"/>
				<Item Name="Clear Errors.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Clear Errors.vi"/>
				<Item Name="Error Cluster From Error Code.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Error Cluster From Error Code.vi"/>
				<Item Name="NI_AALBase.lvlib" Type="Library" URL="/&lt;vilib&gt;/Analysis/NI_AALBase.lvlib"/>
				<Item Name="NI_FileType.lvlib" Type="Library" URL="/&lt;vilib&gt;/Utility/lvfile.llb/NI_FileType.lvlib"/>
				<Item Name="NI_PackedLibraryUtility.lvlib" Type="Library" URL="/&lt;vilib&gt;/Utility/LVLibp/NI_PackedLibraryUtility.lvlib"/>
				<Item Name="Space Constant.vi" Type="VI" URL="/&lt;vilib&gt;/dlg_ctls.llb/Space Constant.vi"/>
				<Item Name="subTimeDelay.vi" Type="VI" URL="/&lt;vilib&gt;/express/express execution control/TimeDelayBlock.llb/subTimeDelay.vi"/>
				<Item Name="Trim Whitespace One-Sided.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Trim Whitespace One-Sided.vi"/>
				<Item Name="Trim Whitespace.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Trim Whitespace.vi"/>
				<Item Name="UDP Multicast Open.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/tcp.llb/UDP Multicast Open.vi"/>
				<Item Name="UDP Multicast Read-Only Open.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/tcp.llb/UDP Multicast Read-Only Open.vi"/>
				<Item Name="UDP Multicast Read-Write Open.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/tcp.llb/UDP Multicast Read-Write Open.vi"/>
				<Item Name="UDP Multicast Write-Only Open.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/tcp.llb/UDP Multicast Write-Only Open.vi"/>
				<Item Name="whitespace.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/whitespace.ctl"/>
				<Item Name="Write Delimited Spreadsheet (DBL).vi" Type="VI" URL="/&lt;vilib&gt;/Utility/file.llb/Write Delimited Spreadsheet (DBL).vi"/>
				<Item Name="Write Delimited Spreadsheet (I64).vi" Type="VI" URL="/&lt;vilib&gt;/Utility/file.llb/Write Delimited Spreadsheet (I64).vi"/>
				<Item Name="Write Delimited Spreadsheet (string).vi" Type="VI" URL="/&lt;vilib&gt;/Utility/file.llb/Write Delimited Spreadsheet (string).vi"/>
				<Item Name="Write Delimited Spreadsheet.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/file.llb/Write Delimited Spreadsheet.vi"/>
				<Item Name="Write Spreadsheet String.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/file.llb/Write Spreadsheet String.vi"/>
				<Item Name="XNET Clear.vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Clear.vi"/>
				<Item Name="XNET Convert (Byte Array to Frame CAN).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Byte Array to Frame CAN).vi"/>
				<Item Name="XNET Convert (Byte Array to Frame FlexRay).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Byte Array to Frame FlexRay).vi"/>
				<Item Name="XNET Convert (Byte Array to Frame LIN).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Byte Array to Frame LIN).vi"/>
				<Item Name="XNET Convert (Byte Array to Frame Raw).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Byte Array to Frame Raw).vi"/>
				<Item Name="XNET Convert (Frame CAN to Byte Array).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Frame CAN to Byte Array).vi"/>
				<Item Name="XNET Convert (Frame CAN to Signal).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Frame CAN to Signal).vi"/>
				<Item Name="XNET Convert (Frame FlexRay to Byte Array).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Frame FlexRay to Byte Array).vi"/>
				<Item Name="XNET Convert (Frame FlexRay to Signal).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Frame FlexRay to Signal).vi"/>
				<Item Name="XNET Convert (Frame LIN to Byte Array).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Frame LIN to Byte Array).vi"/>
				<Item Name="XNET Convert (Frame LIN to Signal).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Frame LIN to Signal).vi"/>
				<Item Name="XNET Convert (Frame Raw to Byte Array).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Frame Raw to Byte Array).vi"/>
				<Item Name="XNET Convert (Frame Raw to Signal).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Frame Raw to Signal).vi"/>
				<Item Name="XNET Convert (Signal to Frame CAN).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Signal to Frame CAN).vi"/>
				<Item Name="XNET Convert (Signal to Frame FlexRay).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Signal to Frame FlexRay).vi"/>
				<Item Name="XNET Convert (Signal to Frame LIN).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Signal to Frame LIN).vi"/>
				<Item Name="XNET Convert (Signal to Frame Raw).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert (Signal to Frame Raw).vi"/>
				<Item Name="XNET Convert.vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Convert.vi"/>
				<Item Name="XNET Create Session (Conversion).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Conversion).vi"/>
				<Item Name="XNET Create Session (Frame Input Queued).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Frame Input Queued).vi"/>
				<Item Name="XNET Create Session (Frame Input Single-point).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Frame Input Single-point).vi"/>
				<Item Name="XNET Create Session (Frame Input Stream).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Frame Input Stream).vi"/>
				<Item Name="XNET Create Session (Frame Output Queued).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Frame Output Queued).vi"/>
				<Item Name="XNET Create Session (Frame Output Single-point).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Frame Output Single-point).vi"/>
				<Item Name="XNET Create Session (Frame Output Stream).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Frame Output Stream).vi"/>
				<Item Name="XNET Create Session (Generic).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Generic).vi"/>
				<Item Name="XNET Create Session (PDU Input Queued).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (PDU Input Queued).vi"/>
				<Item Name="XNET Create Session (PDU Input Single-point).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (PDU Input Single-point).vi"/>
				<Item Name="XNET Create Session (PDU Output Queued).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (PDU Output Queued).vi"/>
				<Item Name="XNET Create Session (PDU Output Single-point).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (PDU Output Single-point).vi"/>
				<Item Name="XNET Create Session (Signal Input Single-point).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Signal Input Single-point).vi"/>
				<Item Name="XNET Create Session (Signal Input Waveform).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Signal Input Waveform).vi"/>
				<Item Name="XNET Create Session (Signal Input XY).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Signal Input XY).vi"/>
				<Item Name="XNET Create Session (Signal Output Single-point).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Signal Output Single-point).vi"/>
				<Item Name="XNET Create Session (Signal Output Waveform).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Signal Output Waveform).vi"/>
				<Item Name="XNET Create Session (Signal Output XY).vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session (Signal Output XY).vi"/>
				<Item Name="XNET Create Session.vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Create Session.vi"/>
				<Item Name="XNET Fill In Error Info.vi" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Fill In Error Info.vi"/>
				<Item Name="XNET Frame CAN.ctl" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Frame CAN.ctl"/>
				<Item Name="XNET Frame FlexRay.ctl" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Frame FlexRay.ctl"/>
				<Item Name="XNET Frame LIN.ctl" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Frame LIN.ctl"/>
				<Item Name="XNET Frame Type CAN.ctl" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Frame Type CAN.ctl"/>
				<Item Name="XNET Frame Type FlexRay.ctl" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Frame Type FlexRay.ctl"/>
				<Item Name="XNET Frame Type LIN.ctl" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Frame Type LIN.ctl"/>
				<Item Name="XNET Mode.ctl" Type="VI" URL="/&lt;vilib&gt;/xnet/xnet.llb/XNET Mode.ctl"/>
			</Item>
			<Item Name="canlib32.dll" Type="Document" URL="canlib32.dll">
				<Property Name="NI.PreserveRelativePath" Type="Bool">true</Property>
			</Item>
			<Item Name="lvanlys.dll" Type="Document" URL="/&lt;resource&gt;/lvanlys.dll"/>
			<Item Name="nixlvapi.dll" Type="Document" URL="nixlvapi.dll">
				<Property Name="NI.PreserveRelativePath" Type="Bool">true</Property>
			</Item>
		</Item>
		<Item Name="程序生成规范" Type="Build"/>
	</Item>
</Project>
