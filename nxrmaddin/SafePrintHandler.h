/* 
	feature:  
		print nxl-doc itself as output file should keep the output file encrypted

	Imple:
		map hdc -> hPrint;  hPrint is str["Microsoft Print to PDF"]

		hooked StartDoc, to get JobID as the reutrned value (>=0)
		
		hooked EndDoc, using EnumJobNamedProperties to get Path;


	Hisotry :
	- 7/06/2020:
		add m_hDenied to hold the HDC that was created by API: CreateDCW,
		We don't want to notify user here, and delay the notification to API: StartDocW;
		WARNING RISK:  if HDC returned by CreateDCW to call ::fork or ::duplicate, current code can't deal with it in the later ::StartDocW.
	- 7/02/2020
		record the nxl-template file path for each Printer job, to fix a bug 62046 
			hdc -> nxl-template;
	- 6/24/2020
		problem, how to make sure outputfile has finished writing?
			wait until, there no not ERROR_SHARING_VIOLATION in ::MoveFileExW
	- 6/22/2020
		feature enhanece, move the printed file out to an tmp fodler, and then finishe Encyrpt there. and Move it back
	- 6/17/2020
		add Enforce_Security_Policy,  as alog_template, using ALLOW_ONLY type
		add Is_Printer_Name_In_Trusted_List as a white list
	- 6/15/2020
		change class name to SafePrintHandler, since in additon to PDF, xps should also handle by this same method
		add function Is_Printer_Name_In_Black_List();
		for nxl file, some printer can not be supported, otherwise date leak triggered, for intance, One-note;
	- 6/11/2020: for win7 
		thers are not function WINSPOOL.DRV!EnumJobNamedProperties;
		so make it as dynamic calling

		

*/
#pragma once

/*
	- only for nxl file
*/
class SafePrintHandler
{
public:
	static SafePrintHandler& Instance() {
		static SafePrintHandler h;
		return h;
	}

	typedef enum _Printer_Result {
		// do not use
		Result_False_Not_In_Trused_List = 0x0100,
		Result_False_Match_In_Prohibited_List,
		// allow to use
		Result_True_General = 0x0200,
		Result_True_Support_Safe_Print
	}Printer_Result;

private:
	SafePrintHandler();
	~SafePrintHandler();

	struct Value {
		int jobID;
		std::wstring printerName;

		Value() :jobID(-1) {}
		Value(int id) :jobID(id) {}
		Value(int id, const std::wstring printer_name) :jobID(id), printerName(printer_name) {}
	};

public:
	bool Modify_JobID(HDC h, int jobID); // requier h must have been recorded
	int Get_JobID(HDC h); // requier h must have been recorded
	Printer_Result Enforce_Security_Policy(const std::wstring& printer_name);
	Printer_Result Enforce_Security_Policy(HDC h);

public: // Data
	inline bool Contain(HDC h) { return m_hs.count(h) != 0; }

	

	inline void Insert(HDC h, int jobID,const std::wstring printer_name) { m_hs[h] = Value(jobID,printer_name); }
	inline void Insert(HDC h, const std::wstring& nxl_path) { m_h2nxl[h] = nxl_path; }
	inline const std::wstring& GetNxlPath(HDC h) { return m_h2nxl[h]; }
	void Remove(HDC h);
	bool GetOutputPath_IfHDCIsValid(HDC h, std::vector<std::wstring>& path);

	bool GetNxltempaltePath_IfHDCIsValid(HDC h, std::wstring& path);

	// generate a new nxl file, in the same folder of pdf_path
	// nxl's attr will sync from [current_nxl_path]
	// new nxl file name is [pdf].nxl
	std::wstring Convert_PrintedFile_To_NXL(const std::wstring& pdf_path,const std::wstring& current_nxl_path);

private:
	bool get_user_tmp_folder(std::wstring& path);
	void clear_user_tmp_folder();
	bool move_file_to_tmp_folder(const std::wstring& org_plain_file_path, std::wstring& out_moved_file_path);
	// make sure not clash with the exsited in folder
	std::wstring get_no_clash_nxl_file_path(const std::wstring& folder, const std::wstring& file_name);

protected: // routines
	bool Is_Printer_Name_In_Trusted_List(const std::wstring printer_name);
	bool Is_Printer_Name_In_Prohibited_List(const std::wstring printer_name);
	bool Is_Printer_Name_Support_Safe_Print(const std::wstring printer_name);


private:
	std::map<HDC, SafePrintHandler::Value> m_hs;
public:
	CRITICAL_SECTION _lock;
	std::map<HDC, std::wstring> m_h2nxl;
};

