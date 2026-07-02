<?xml version='1.0' encoding='UTF-8'?>
<Project Type="Project" LVVersion="24008000">
	<Item Name="我的电脑" Type="My Computer">
		<Property Name="NI.SortType" Type="Int">3</Property>
		<Property Name="server.app.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="server.control.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="server.tcp.enabled" Type="Bool">false</Property>
		<Property Name="server.tcp.port" Type="Int">0</Property>
		<Property Name="server.tcp.serviceName" Type="Str">我的电脑/VI服务器</Property>
		<Property Name="server.tcp.serviceName.default" Type="Str">我的电脑/VI服务器</Property>
		<Property Name="server.vi.callsEnabled" Type="Bool">true</Property>
		<Property Name="server.vi.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="specify.custom.address" Type="Bool">false</Property>
		<Item Name="Ctrl" Type="Folder" URL="../Ctrl">
			<Property Name="NI.DISK" Type="Bool">true</Property>
		</Item>
		<Item Name="PLC_Op.vi" Type="VI" URL="../PLC_Op.vi"/>
		<Item Name="Origin.vi" Type="VI" URL="../Origin.vi"/>
		<Item Name="PIN Count.vi" Type="VI" URL="../PIN Count.vi"/>
		<Item Name="PLC Init.vi" Type="VI" URL="../PLC Init.vi"/>
		<Item Name="Position.vi" Type="VI" URL="../Position.vi"/>
		<Item Name="Power.vi" Type="VI" URL="../Power.vi"/>
		<Item Name="Read_MES_Status.vi" Type="VI" URL="../Read_MES_Status.vi"/>
		<Item Name="Read_RunMode.vi" Type="VI" URL="../Read_RunMode.vi"/>
		<Item Name="Read_SN.vi" Type="VI" URL="../Read_SN.vi"/>
		<Item Name="Wait TestStatus.vi" Type="VI" URL="../Wait TestStatus.vi"/>
		<Item Name="StartTest.vi" Type="VI" URL="../StartTest.vi"/>
		<Item Name="StopTest.vi" Type="VI" URL="../StopTest.vi"/>
		<Item Name="Wait LoadStatus.vi" Type="VI" URL="../Wait LoadStatus.vi"/>
		<Item Name="Write_Result.vi" Type="VI" URL="../Write_Result.vi"/>
		<Item Name="CornerReflector.vi" Type="VI" URL="../CornerReflector.vi"/>
		<Item Name="PLC Close.vi" Type="VI" URL="../PLC Close.vi"/>
		<Item Name="Enter TestMode.vi" Type="VI" URL="../Enter TestMode.vi"/>
		<Item Name="Exit TestMode.vi" Type="VI" URL="../Exit TestMode.vi"/>
		<Item Name="Wait StartButton.vi" Type="VI" URL="../Wait StartButton.vi"/>
		<Item Name="Wait DUT Load.vi" Type="VI" URL="../Wait DUT Load.vi"/>
		<Item Name="Scan Ok.vi" Type="VI" URL="../Scan Ok.vi"/>
		<Item Name="TS Termination Op.vi" Type="VI" URL="../TS Termination Op.vi"/>
		<Item Name="plc time.vi" Type="VI" URL="../plc time.vi"/>
		<Item Name="Position Test.vi" Type="VI" URL="../Position Test.vi"/>
		<Item Name="Counter.vi" Type="VI" URL="../Counter.vi"/>
		<Item Name="Counter Demo.vi" Type="VI" URL="../Counter Demo.vi"/>
		<Item Name="依赖关系" Type="Dependencies">
			<Item Name="vi.lib" Type="Folder">
				<Item Name="TestStand - Status Monitor.ctl" Type="VI" URL="/&lt;vilib&gt;/addons/TestStand/_TSUtility.llb/TestStand - Status Monitor.ctl"/>
				<Item Name="S7NetCom.lvlib" Type="Library" URL="/&lt;vilib&gt;/Hampel Software Engineering/S7NetCom/S7NetCom.lvlib"/>
				<Item Name="Error Cluster From Error Code.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Error Cluster From Error Code.vi"/>
				<Item Name="LVDateTimeRec.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/miscctls.llb/LVDateTimeRec.ctl"/>
				<Item Name="TestStand - Close Termination Monitor.vi" Type="VI" URL="/&lt;vilib&gt;/addons/TestStand/_TSUtility.llb/TestStand - Close Termination Monitor.vi"/>
				<Item Name="TestStand - Get Termination Monitor Status.vi" Type="VI" URL="/&lt;vilib&gt;/addons/TestStand/_TSUtility.llb/TestStand - Get Termination Monitor Status.vi"/>
				<Item Name="TestStand - Initialize Termination Monitor.vi" Type="VI" URL="/&lt;vilib&gt;/addons/TestStand/_TSUtility.llb/TestStand - Initialize Termination Monitor.vi"/>
				<Item Name="subTimeDelay.vi" Type="VI" URL="/&lt;vilib&gt;/express/express execution control/TimeDelayBlock.llb/subTimeDelay.vi"/>
				<Item Name="DAQmx Read.vi" Type="VI" URL="/&lt;vilib&gt;/DAQmx/read.llb/DAQmx Read.vi"/>
				<Item Name="DAQmx Fill In Error Info.vi" Type="VI" URL="/&lt;vilib&gt;/DAQmx/miscellaneous.llb/DAQmx Fill In Error Info.vi"/>
				<Item Name="DAQmx Read (Counter DBL 1Chan 1Samp).vi" Type="VI" URL="/&lt;vilib&gt;/DAQmx/read.llb/DAQmx Read (Counter DBL 1Chan 1Samp).vi"/>
				<Item Name="DAQmx Create Virtual Channel.vi" Type="VI" URL="/&lt;vilib&gt;/DAQmx/create/channels.llb/DAQmx Create Virtual Channel.vi"/>
				<Item Name="DAQmx Create Channel (CI-Position-Angular Encoder).vi" Type="VI" URL="/&lt;vilib&gt;/DAQmx/create/channels.llb/DAQmx Create Channel (CI-Position-Angular Encoder).vi"/>
				<Item Name="DAQmx Start Task.vi" Type="VI" URL="/&lt;vilib&gt;/DAQmx/configure/task.llb/DAQmx Start Task.vi"/>
				<Item Name="DAQmx Stop Task.vi" Type="VI" URL="/&lt;vilib&gt;/DAQmx/configure/task.llb/DAQmx Stop Task.vi"/>
				<Item Name="DAQmx Clear Task.vi" Type="VI" URL="/&lt;vilib&gt;/DAQmx/configure/task.llb/DAQmx Clear Task.vi"/>
				<Item Name="Write PLC Data Scalar.vi" Type="VI" URL="/&lt;vilib&gt;/Hampel Software Engineering/S7NetCom/Source/API/Write PLC Data Scalar.vi"/>
				<Item Name="Read PLC Data Array.vi" Type="VI" URL="/&lt;vilib&gt;/Hampel Software Engineering/S7NetCom/Source/API/Read PLC Data Array.vi"/>
				<Item Name="Read PLC Data Scalar.vi" Type="VI" URL="/&lt;vilib&gt;/Hampel Software Engineering/S7NetCom/Source/API/Read PLC Data Scalar.vi"/>
			</Item>
			<Item Name="nilvaiu.dll" Type="Document" URL="nilvaiu.dll">
				<Property Name="NI.PreserveRelativePath" Type="Bool">true</Property>
			</Item>
		</Item>
		<Item Name="程序生成规范" Type="Build"/>
	</Item>
</Project>
