<?xml version='1.0' encoding='UTF-8'?>
<Project Type="Project" LVVersion="24008000">
	<Property Name="NI.LV.All.SaveVersion" Type="Str">24.0</Property>
	<Property Name="NI.LV.All.SourceOnly" Type="Bool">true</Property>
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
		<Item Name="Ctrls" Type="Folder" URL="../Ctrls">
			<Property Name="NI.DISK" Type="Bool">true</Property>
		</Item>
		<Item Name="DailyCount.vi" Type="VI" URL="../DailyCount.vi"/>
		<Item Name="DataLog Path.vi" Type="VI" URL="../DataLog Path.vi"/>
		<Item Name="Enter SN.vi" Type="VI" URL="../Enter SN.vi"/>
		<Item Name="error.vi" Type="VI" URL="../error.vi"/>
		<Item Name="FormatExcelArray.vi" Type="VI" URL="../FormatExcelArray.vi"/>
		<Item Name="LoadGlobalConfigure.vi" Type="VI" URL="../LoadGlobalConfigure.vi"/>
		<Item Name="pop.vi" Type="VI" URL="../pop.vi"/>
		<Item Name="MoveFile.vi" Type="VI" URL="../MoveFile.vi"/>
		<Item Name="Log Path.vi" Type="VI" URL="../Log Path.vi"/>
		<Item Name="SeqParamsReadExcel.vi" Type="VI" URL="../SeqParamsReadExcel.vi"/>
		<Item Name="WRYield.vi" Type="VI" URL="../WRYield.vi"/>
		<Item Name="Excel.lvlibp" Type="LVLibp" URL="../Excel.lvlibp">
			<Item Name="Build string table (excel).vi" Type="VI" URL="../Excel.lvlibp/Build string table (excel).vi"/>
			<Item Name="Calc Long Word Padded Width (excel).vi" Type="VI" URL="../Excel.lvlibp/Calc Long Word Padded Width (excel).vi"/>
			<Item Name="Check Color Table Size (excel).vi" Type="VI" URL="../Excel.lvlibp/Check Color Table Size (excel).vi"/>
			<Item Name="Check Data Size (excel).vi" Type="VI" URL="../Excel.lvlibp/Check Data Size (excel).vi"/>
			<Item Name="Check Path (excel).vi" Type="VI" URL="../Excel.lvlibp/Check Path (excel).vi"/>
			<Item Name="Clear Errors.vi" Type="VI" URL="../Excel.lvlibp/1abvi3w/vi.lib/Utility/error.llb/Clear Errors.vi"/>
			<Item Name="Close Specific WorkBook.vi" Type="VI" URL="../Excel.lvlibp/Close Specific WorkBook.vi"/>
			<Item Name="Convert OLE Color (excel).vi" Type="VI" URL="../Excel.lvlibp/Convert OLE Color (excel).vi"/>
			<Item Name="Convert OLE Color (word).vi" Type="VI" URL="../Excel.lvlibp/Convert OLE Color (word).vi"/>
			<Item Name="Directory of Top Level VI (excel).vi" Type="VI" URL="../Excel.lvlibp/Directory of Top Level VI (excel).vi"/>
			<Item Name="Error Cluster From Error Code.vi" Type="VI" URL="../Excel.lvlibp/1abvi3w/vi.lib/Utility/error.llb/Error Cluster From Error Code.vi"/>
			<Item Name="Excel Add Worksheet.vi" Type="VI" URL="../Excel.lvlibp/Excel Add Worksheet.vi"/>
			<Item Name="Excel Bring to Front.vi" Type="VI" URL="../Excel.lvlibp/Excel Bring to Front.vi"/>
			<Item Name="Excel Find and Replace (str).vi" Type="VI" URL="../Excel.lvlibp/Excel Find and Replace (str).vi"/>
			<Item Name="Excel Format Image.vi" Type="VI" URL="../Excel.lvlibp/Excel Format Image.vi"/>
			<Item Name="Excel Get Cell Font.vi" Type="VI" URL="../Excel.lvlibp/Excel Get Cell Font.vi"/>
			<Item Name="Excel Get Last Column.vi" Type="VI" URL="../Excel.lvlibp/Excel Get Last Column.vi"/>
			<Item Name="Excel Get Last Row.vi" Type="VI" URL="../Excel.lvlibp/Excel Get Last Row.vi"/>
			<Item Name="Excel Insert Cells.vi" Type="VI" URL="../Excel.lvlibp/Excel Insert Cells.vi"/>
			<Item Name="Excel Insert Formula.vi" Type="VI" URL="../Excel.lvlibp/Excel Insert Formula.vi"/>
			<Item Name="Excel Insert Graph.vi" Type="VI" URL="../Excel.lvlibp/Excel Insert Graph.vi"/>
			<Item Name="Excel Insert Object.vi" Type="VI" URL="../Excel.lvlibp/Excel Insert Object.vi"/>
			<Item Name="Excel Merge Cells.vi" Type="VI" URL="../Excel.lvlibp/Excel Merge Cells.vi"/>
			<Item Name="Excel Rename Worksheet.vi" Type="VI" URL="../Excel.lvlibp/Excel Rename Worksheet.vi"/>
			<Item Name="Excel Send Workbook.vi" Type="VI" URL="../Excel.lvlibp/Excel Send Workbook.vi"/>
			<Item Name="Excel Set CB.vi" Type="VI" URL="../Excel.lvlibp/Excel Set CB.vi"/>
			<Item Name="Excel Set Cell Alignment.vi" Type="VI" URL="../Excel.lvlibp/Excel Set Cell Alignment.vi"/>
			<Item Name="Excel Set Cell Color and Border.vi" Type="VI" URL="../Excel.lvlibp/Excel Set Cell Color and Border.vi"/>
			<Item Name="Excel Set Cell Dimension.vi" Type="VI" URL="../Excel.lvlibp/Excel Set Cell Dimension.vi"/>
			<Item Name="Excel Set Cell Font.vi" Type="VI" URL="../Excel.lvlibp/Excel Set Cell Font.vi"/>
			<Item Name="Excel Set Cell Format.vi" Type="VI" URL="../Excel.lvlibp/Excel Set Cell Format.vi"/>
			<Item Name="Excel Set Chart Axis(Graph) Font.vi" Type="VI" URL="../Excel.lvlibp/Excel Set Chart Axis(Graph) Font.vi"/>
			<Item Name="Excel Set Chart(Graph) Colors.vi" Type="VI" URL="../Excel.lvlibp/Excel Set Chart(Graph) Colors.vi"/>
			<Item Name="Excel Set Chart(Graph) Scale.vi" Type="VI" URL="../Excel.lvlibp/Excel Set Chart(Graph) Scale.vi"/>
			<Item Name="Excel Set Page Numbering.vi" Type="VI" URL="../Excel.lvlibp/Excel Set Page Numbering.vi"/>
			<Item Name="Excel Set Paper Size.vi" Type="VI" URL="../Excel.lvlibp/Excel Set Paper Size.vi"/>
			<Item Name="Excel Sort Data.vi" Type="VI" URL="../Excel.lvlibp/Excel Sort Data.vi"/>
			<Item Name="Excel_Calc_Bottom_Row#.vi" Type="VI" URL="../Excel.lvlibp/Excel_Calc_Bottom_Row#.vi"/>
			<Item Name="Excel_Chart_Font.vi" Type="VI" URL="../Excel.lvlibp/Excel_Chart_Font.vi"/>
			<Item Name="Excel_Clear_Worksheet.vi" Type="VI" URL="../Excel.lvlibp/Excel_Clear_Worksheet.vi"/>
			<Item Name="Excel_Color_to_RGB.vi" Type="VI" URL="../Excel.lvlibp/Excel_Color_to_RGB.vi"/>
			<Item Name="Excel_Create_Workbook.vi" Type="VI" URL="../Excel.lvlibp/Excel_Create_Workbook.vi"/>
			<Item Name="Excel_Font_Wizard.vi" Type="VI" URL="../Excel.lvlibp/Excel_Font_Wizard.vi"/>
			<Item Name="Excel_Get_Names_from_Template.vi" Type="VI" URL="../Excel.lvlibp/Excel_Get_Names_from_Template.vi"/>
			<Item Name="Excel_Get_Properties.vi" Type="VI" URL="../Excel.lvlibp/Excel_Get_Properties.vi"/>
			<Item Name="Excel_Get_Range.vi" Type="VI" URL="../Excel.lvlibp/Excel_Get_Range.vi"/>
			<Item Name="Excel_Get_Reference_From_Range.vi" Type="VI" URL="../Excel.lvlibp/Excel_Get_Reference_From_Range.vi"/>
			<Item Name="Excel_Insert_Chart.vi" Type="VI" URL="../Excel.lvlibp/Excel_Insert_Chart.vi"/>
			<Item Name="Excel_Insert_Footer.vi" Type="VI" URL="../Excel.lvlibp/Excel_Insert_Footer.vi"/>
			<Item Name="Excel_Insert_header.vi" Type="VI" URL="../Excel.lvlibp/Excel_Insert_header.vi"/>
			<Item Name="Excel_Insert_Link.vi" Type="VI" URL="../Excel.lvlibp/Excel_Insert_Link.vi"/>
			<Item Name="Excel_Insert_Table.vi" Type="VI" URL="../Excel.lvlibp/Excel_Insert_Table.vi"/>
			<Item Name="Excel_Insert_Textbox.vi" Type="VI" URL="../Excel.lvlibp/Excel_Insert_Textbox.vi"/>
			<Item Name="Excel_LVtoXL_Color.vi" Type="VI" URL="../Excel.lvlibp/Excel_LVtoXL_Color.vi"/>
			<Item Name="Excel_New_Page.vi" Type="VI" URL="../Excel.lvlibp/Excel_New_Page.vi"/>
			<Item Name="Excel_Page_Setup.vi" Type="VI" URL="../Excel.lvlibp/Excel_Page_Setup.vi"/>
			<Item Name="Excel_Print.vi" Type="VI" URL="../Excel.lvlibp/Excel_Print.vi"/>
			<Item Name="Excel_Quit.vi" Type="VI" URL="../Excel.lvlibp/Excel_Quit.vi"/>
			<Item Name="Excel_Quit_Graph.vi" Type="VI" URL="../Excel.lvlibp/Excel_Quit_Graph.vi"/>
			<Item Name="Excel_Save_Workbook.vi" Type="VI" URL="../Excel.lvlibp/Excel_Save_Workbook.vi"/>
			<Item Name="Excel_Set_Chart_Gridelines.vi" Type="VI" URL="../Excel.lvlibp/Excel_Set_Chart_Gridelines.vi"/>
			<Item Name="Excel_Set_Font.vi" Type="VI" URL="../Excel.lvlibp/Excel_Set_Font.vi"/>
			<Item Name="Excel_Set_Header_Font.vi" Type="VI" URL="../Excel.lvlibp/Excel_Set_Header_Font.vi"/>
			<Item Name="Excel_Set_Orientation.vi" Type="VI" URL="../Excel.lvlibp/Excel_Set_Orientation.vi"/>
			<Item Name="Excel_Set_Scale.vi" Type="VI" URL="../Excel.lvlibp/Excel_Set_Scale.vi"/>
			<Item Name="Excel_Update_Chart(Graph).vi" Type="VI" URL="../Excel.lvlibp/Excel_Update_Chart(Graph).vi"/>
			<Item Name="ExcelDemo.vi" Type="VI" URL="../Excel.lvlibp/ExcelDemo.vi"/>
			<Item Name="Find First Error and Warning.vi" Type="VI" URL="../Excel.lvlibp/Find First Error and Warning.vi"/>
			<Item Name="Flip and Pad for Picture Control (excel).vi" Type="VI" URL="../Excel.lvlibp/Flip and Pad for Picture Control (excel).vi"/>
			<Item Name="font.ctl" Type="VI" URL="../Excel.lvlibp/font.ctl"/>
			<Item Name="Open Excel Application.vi" Type="VI" URL="../Excel.lvlibp/Open Excel Application.vi"/>
			<Item Name="Open Specific WorkBook.vi" Type="VI" URL="../Excel.lvlibp/Open Specific WorkBook.vi"/>
			<Item Name="Open Specific WorkSheet.vi" Type="VI" URL="../Excel.lvlibp/Open Specific WorkSheet.vi"/>
			<Item Name="Query Available Printers.vi" Type="VI" URL="../Excel.lvlibp/Query Available Printers.vi"/>
			<Item Name="Rang To Row Col.vi" Type="VI" URL="../Excel.lvlibp/Rang To Row Col.vi"/>
			<Item Name="Read BMP File (excel).vi" Type="VI" URL="../Excel.lvlibp/Read BMP File (excel).vi"/>
			<Item Name="Read BMP File Data (excel).vi" Type="VI" URL="../Excel.lvlibp/Read BMP File Data (excel).vi"/>
			<Item Name="Read BMP Header Info (excel).vi" Type="VI" URL="../Excel.lvlibp/Read BMP Header Info (excel).vi"/>
			<Item Name="Read Cell Value.vi" Type="VI" URL="../Excel.lvlibp/Read Cell Value.vi"/>
			<Item Name="Row Col To Range.vi" Type="VI" URL="../Excel.lvlibp/Row Col To Range.vi"/>
			<Item Name="Set Cell Value.vi" Type="VI" URL="../Excel.lvlibp/Set Cell Value.vi"/>
		</Item>
		<Item Name="ReadAngle.vi" Type="VI" URL="../ReadAngle.vi"/>
		<Item Name="依赖关系" Type="Dependencies">
			<Item Name="vi.lib" Type="Folder">
				<Item Name="8.6CompatibleGlobalVar.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/config.llb/8.6CompatibleGlobalVar.vi"/>
				<Item Name="Bit-array To Byte-array.vi" Type="VI" URL="/&lt;vilib&gt;/picture/pictutil.llb/Bit-array To Byte-array.vi"/>
				<Item Name="Built App File Layout.vi" Type="VI" URL="/&lt;vilib&gt;/AppBuilder/Built App File Layout.vi"/>
				<Item Name="Calc Long Word Padded Width.vi" Type="VI" URL="/&lt;vilib&gt;/picture/bmp.llb/Calc Long Word Padded Width.vi"/>
				<Item Name="Check Color Table Size.vi" Type="VI" URL="/&lt;vilib&gt;/picture/jpeg.llb/Check Color Table Size.vi"/>
				<Item Name="Check Data Size.vi" Type="VI" URL="/&lt;vilib&gt;/picture/jpeg.llb/Check Data Size.vi"/>
				<Item Name="Check File Permissions.vi" Type="VI" URL="/&lt;vilib&gt;/picture/jpeg.llb/Check File Permissions.vi"/>
				<Item Name="Check if File or Folder Exists.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/libraryn.llb/Check if File or Folder Exists.vi"/>
				<Item Name="Check Path.vi" Type="VI" URL="/&lt;vilib&gt;/picture/jpeg.llb/Check Path.vi"/>
				<Item Name="Clear Errors.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Clear Errors.vi"/>
				<Item Name="Close Registry Key.vi" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Close Registry Key.vi"/>
				<Item Name="Create ActiveX Event Queue.vi" Type="VI" URL="/&lt;vilib&gt;/Platform/ax-events.llb/Create ActiveX Event Queue.vi"/>
				<Item Name="Create Error Clust.vi" Type="VI" URL="/&lt;vilib&gt;/Platform/ax-events.llb/Create Error Clust.vi"/>
				<Item Name="Create Mask By Alpha.vi" Type="VI" URL="/&lt;vilib&gt;/picture/picture.llb/Create Mask By Alpha.vi"/>
				<Item Name="Destroy ActiveX Event Queue.vi" Type="VI" URL="/&lt;vilib&gt;/Platform/ax-events.llb/Destroy ActiveX Event Queue.vi"/>
				<Item Name="Directory of Top Level VI.vi" Type="VI" URL="/&lt;vilib&gt;/picture/jpeg.llb/Directory of Top Level VI.vi"/>
				<Item Name="Error Cluster From Error Code.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Error Cluster From Error Code.vi"/>
				<Item Name="Escape Characters for HTTP.vi" Type="VI" URL="/&lt;vilib&gt;/printing/PathToURL.llb/Escape Characters for HTTP.vi"/>
				<Item Name="EventData.ctl" Type="VI" URL="/&lt;vilib&gt;/Platform/ax-events.llb/EventData.ctl"/>
				<Item Name="Flip and Pad for Picture Control.vi" Type="VI" URL="/&lt;vilib&gt;/picture/bmp.llb/Flip and Pad for Picture Control.vi"/>
				<Item Name="Generate Temporary File Path.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/libraryn.llb/Generate Temporary File Path.vi"/>
				<Item Name="Get File Extension.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/libraryn.llb/Get File Extension.vi"/>
				<Item Name="Get LV Class Default Value.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/LVClass/Get LV Class Default Value.vi"/>
				<Item Name="Handle Open Word or Excel File.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/NIReport.llb/Toolkit/Handle Open Word or Excel File.vi"/>
				<Item Name="imagedata.ctl" Type="VI" URL="/&lt;vilib&gt;/picture/picture.llb/imagedata.ctl"/>
				<Item Name="List Directory and LLBs.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/libraryn.llb/List Directory and LLBs.vi"/>
				<Item Name="NI_Excel.lvclass" Type="LVClass" URL="/&lt;vilib&gt;/Utility/NIReport.llb/Excel/NI_Excel.lvclass"/>
				<Item Name="NI_FileType.lvlib" Type="Library" URL="/&lt;vilib&gt;/Utility/lvfile.llb/NI_FileType.lvlib"/>
				<Item Name="NI_HTML.lvclass" Type="LVClass" URL="/&lt;vilib&gt;/Utility/NIReport.llb/HTML/NI_HTML.lvclass"/>
				<Item Name="NI_LVConfig.lvlib" Type="Library" URL="/&lt;vilib&gt;/Utility/config.llb/NI_LVConfig.lvlib"/>
				<Item Name="NI_PackedLibraryUtility.lvlib" Type="Library" URL="/&lt;vilib&gt;/Utility/LVLibp/NI_PackedLibraryUtility.lvlib"/>
				<Item Name="NI_report.lvclass" Type="LVClass" URL="/&lt;vilib&gt;/Utility/NIReport.llb/NI_report.lvclass"/>
				<Item Name="NI_ReportGenerationCore.lvlib" Type="Library" URL="/&lt;vilib&gt;/Utility/NIReport.llb/NI_ReportGenerationCore.lvlib"/>
				<Item Name="NI_ReportGenerationToolkit.lvlib" Type="Library" URL="/&lt;vilib&gt;/addons/_office/NI_ReportGenerationToolkit.lvlib"/>
				<Item Name="NI_Standard Report.lvclass" Type="LVClass" URL="/&lt;vilib&gt;/Utility/NIReport.llb/Standard Report/NI_Standard Report.lvclass"/>
				<Item Name="OccFireType.ctl" Type="VI" URL="/&lt;vilib&gt;/Platform/ax-events.llb/OccFireType.ctl"/>
				<Item Name="Open Registry Key.vi" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Open Registry Key.vi"/>
				<Item Name="Path to URL inner.vi" Type="VI" URL="/&lt;vilib&gt;/printing/PathToURL.llb/Path to URL inner.vi"/>
				<Item Name="Path to URL.vi" Type="VI" URL="/&lt;vilib&gt;/printing/PathToURL.llb/Path to URL.vi"/>
				<Item Name="Read JPEG File.vi" Type="VI" URL="/&lt;vilib&gt;/picture/jpeg.llb/Read JPEG File.vi"/>
				<Item Name="Read PNG File.vi" Type="VI" URL="/&lt;vilib&gt;/picture/png.llb/Read PNG File.vi"/>
				<Item Name="Read Registry Value DWORD.vi" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Read Registry Value DWORD.vi"/>
				<Item Name="Read Registry Value Simple STR.vi" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Read Registry Value Simple STR.vi"/>
				<Item Name="Read Registry Value Simple U32.vi" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Read Registry Value Simple U32.vi"/>
				<Item Name="Read Registry Value Simple.vi" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Read Registry Value Simple.vi"/>
				<Item Name="Read Registry Value STR.vi" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Read Registry Value STR.vi"/>
				<Item Name="Read Registry Value.vi" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Read Registry Value.vi"/>
				<Item Name="Recursive File List.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/libraryn.llb/Recursive File List.vi"/>
				<Item Name="Registry Handle Master.vi" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Registry Handle Master.vi"/>
				<Item Name="Registry refnum.ctl" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Registry refnum.ctl"/>
				<Item Name="Registry RtKey.ctl" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Registry RtKey.ctl"/>
				<Item Name="Registry SAM.ctl" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Registry SAM.ctl"/>
				<Item Name="Registry Simplify Data Type.vi" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Registry Simplify Data Type.vi"/>
				<Item Name="Registry View.ctl" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Registry View.ctl"/>
				<Item Name="Registry WinErr-LVErr.vi" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/Registry WinErr-LVErr.vi"/>
				<Item Name="Search and Replace Pattern.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Search and Replace Pattern.vi"/>
				<Item Name="Space Constant.vi" Type="VI" URL="/&lt;vilib&gt;/dlg_ctls.llb/Space Constant.vi"/>
				<Item Name="STR_ASCII-Unicode.vi" Type="VI" URL="/&lt;vilib&gt;/registry/registry.llb/STR_ASCII-Unicode.vi"/>
				<Item Name="Trim Whitespace One-Sided.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Trim Whitespace One-Sided.vi"/>
				<Item Name="Trim Whitespace.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Trim Whitespace.vi"/>
				<Item Name="Wait On ActiveX Event.vi" Type="VI" URL="/&lt;vilib&gt;/Platform/ax-events.llb/Wait On ActiveX Event.vi"/>
				<Item Name="Wait types.ctl" Type="VI" URL="/&lt;vilib&gt;/Platform/ax-events.llb/Wait types.ctl"/>
				<Item Name="whitespace.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/whitespace.ctl"/>
				<Item Name="Write BMP Data To Buffer.vi" Type="VI" URL="/&lt;vilib&gt;/picture/bmp.llb/Write BMP Data To Buffer.vi"/>
				<Item Name="Write BMP Data.vi" Type="VI" URL="/&lt;vilib&gt;/picture/bmp.llb/Write BMP Data.vi"/>
				<Item Name="Write BMP File.vi" Type="VI" URL="/&lt;vilib&gt;/picture/bmp.llb/Write BMP File.vi"/>
				<Item Name="Write JPEG File.vi" Type="VI" URL="/&lt;vilib&gt;/picture/jpeg.llb/Write JPEG File.vi"/>
				<Item Name="Write PNG File.vi" Type="VI" URL="/&lt;vilib&gt;/picture/png.llb/Write PNG File.vi"/>
			</Item>
			<Item Name="Advapi32.dll" Type="Document" URL="Advapi32.dll">
				<Property Name="NI.PreserveRelativePath" Type="Bool">true</Property>
			</Item>
			<Item Name="kernel32.dll" Type="Document" URL="kernel32.dll">
				<Property Name="NI.PreserveRelativePath" Type="Bool">true</Property>
			</Item>
		</Item>
		<Item Name="程序生成规范" Type="Build"/>
	</Item>
</Project>
