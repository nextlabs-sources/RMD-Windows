#pragma once
#include <vector>
#include "nxrmcomtypes.h"
#include "commonutils.h"

class FileTypeUtils
{

private:
	std::vector<std::wstring> _WinWord;
	std::vector<std::wstring> _PowerPnt;
	std::vector<std::wstring> _Excel;
	std::vector<std::wstring> _Pdf;
	std::vector<std::wstring> _Hoops;
	std::vector<std::wstring> _Audio;
	std::vector<std::wstring> _Video;
	std::vector<std::wstring> _Image;
	std::vector<std::wstring> _Text;
	std::vector<std::wstring> _Vds;

public:
	FileTypeUtils()
	{
		_WinWord.push_back(L".docx");
		_WinWord.push_back(L".doc");
		_WinWord.push_back(L".dot");
		_WinWord.push_back(L".dotx");
		_WinWord.push_back(L".rtf");

		_PowerPnt.push_back(L".pptx");
		_PowerPnt.push_back(L".ppt");
		_PowerPnt.push_back(L".ppsx");
		_PowerPnt.push_back(L".potx");

		_Excel.push_back(L".xlsx");
		_Excel.push_back(L".xls");
		_Excel.push_back(L".xltx");
		_Excel.push_back(L".xlt");
		_Excel.push_back(L".xlsb");

		_Pdf.push_back(L".pdf");

		_Hoops.push_back(L".hsf");
		_Hoops.push_back(L".jt");
		_Hoops.push_back(L".igs");
		_Hoops.push_back(L".stp");
		_Hoops.push_back(L".stl");
		_Hoops.push_back(L".step");
		_Hoops.push_back(L".iges");
		_Hoops.push_back(L".par");
		_Hoops.push_back(L".psm");
		_Hoops.push_back(L".x_t");
		_Hoops.push_back(L".x_b");
		_Hoops.push_back(L".xmt_txt");
		_Hoops.push_back(L".prt");
		_Hoops.push_back(L".neu");
		_Hoops.push_back(L".model");
		_Hoops.push_back(L".3dxml");
		_Hoops.push_back(L".catpart");
		_Hoops.push_back(L".cgr");
		_Hoops.push_back(L".catshape");
		_Hoops.push_back(L".prt");
		_Hoops.push_back(L".sldprt");
		_Hoops.push_back(L".sldasm");
		_Hoops.push_back(L".dwg");
		_Hoops.push_back(L".dxf");
		_Hoops.push_back(L".ipt");

		_Audio.push_back(L".mp3");

		_Video.push_back(L".mp4");

		_Image.push_back(L".png");
		_Image.push_back(L".gif");
		_Image.push_back(L".jpg");
		_Image.push_back(L".bmp");
		_Image.push_back(L".tif");
		_Image.push_back(L".tiff");
		_Image.push_back(L".jpe");


		_Text.push_back(L".cpp");
		_Text.push_back(L".htm");
		_Text.push_back(L".xml");
		_Text.push_back(L".json");
		_Text.push_back(L".h");
		_Text.push_back(L".js");
		_Text.push_back(L".java");
		_Text.push_back(L".err");
		_Text.push_back(L".m");
		_Text.push_back(L".swift");
		_Text.push_back(L".txt");
		_Text.push_back(L".log");
		_Text.push_back(L".sql");
		_Text.push_back(L".c");
		_Text.push_back(L".py");

		_Vds.push_back(L".vds");
	}

public:
	FileType MattchFileType(std::wstring extension)
	{

		FileType res = UnSupport;

		CommonUtils cu;

		for (auto iter = _WinWord.cbegin(); iter != _WinWord.cend(); iter++)
		{
			if (cu.StrIcompare(extension, (*iter))) {
				res = WinWord;
				break;
			}
		}

		for (std::vector<std::wstring>::const_iterator iter = _PowerPnt.cbegin(); iter != _PowerPnt.cend(); iter++)
		{
			if (cu.StrIcompare(extension, (*iter))) {
				res = PowerPnt;
				break;
			}
		}

		for (std::vector<std::wstring>::const_iterator iter = _Excel.cbegin(); iter != _Excel.cend(); iter++)
		{
			if (cu.StrIcompare(extension, (*iter))) {
				res = Excel;
				break;
			}
		}

		for (std::vector<std::wstring>::const_iterator iter = _Pdf.cbegin(); iter != _Pdf.cend(); iter++)
		{
			if (cu.StrIcompare(extension, (*iter))) {
				res = Pdf;
				break;
			}
		}

		for (std::vector<std::wstring>::const_iterator iter = _Hoops.cbegin(); iter != _Hoops.cend(); iter++)
		{
			if (cu.StrIcompare(extension, (*iter))) {
				res = Hoops;
				break;
			}

		}

		for (std::vector<std::wstring>::const_iterator iter = _Audio.cbegin(); iter != _Audio.cend(); iter++)
		{
			if (cu.StrIcompare(extension, (*iter))) {
				res = Audio;
				break;
			}
		}

		for (std::vector<std::wstring>::const_iterator iter = _Video.cbegin(); iter != _Video.cend(); iter++)
		{
			if (cu.StrIcompare(extension, (*iter))) {
				res = Video;
				break;
			}
		}


		for (std::vector<std::wstring>::const_iterator iter = _Image.cbegin(); iter != _Image.cend(); iter++)
		{
			if (cu.StrIcompare(extension, (*iter))) {
				res = Image;
				break;
			}
		}


		for (std::vector<std::wstring>::const_iterator iter = _Text.cbegin(); iter != _Text.cend(); iter++)
		{
			if (cu.StrIcompare(extension, (*iter))) {
				res = Text;
				break;
			}
		}

		for (std::vector<std::wstring>::const_iterator iter = _Vds.cbegin(); iter != _Vds.cend(); iter++)
		{
			if (cu.StrIcompare(extension, (*iter))) {
				res = Vds;
				break;
			}
		}

		return res;
	}


};






