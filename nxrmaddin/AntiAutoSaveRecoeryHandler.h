#pragma once
#include "HookManager.h"


class AntiAutoSaveRecoeryHandler
{
	
public:
	AntiAutoSaveRecoeryHandler() : m_is_osdlp_inited(false) {}

	// make sure get connected with os_dlp
	bool instance_setup();	

	bool register_folder(const std::wstring& src_folder, const std::wstring& dst);
	
	// do it at plugin unloading event;
	void impl_handler();
private:
	typedef bool(_stdcall* init_file_virtualization)(HANDLE* outhandler);
	typedef bool(_stdcall* set_fv_path)(HANDLE handler, const wchar_t* target, const wchar_t* redirected);
private:
	// <src, dst>
	std::map<std::wstring,std::wstring>		m_folders;
	std::vector<HANDLE>						m_hfvs;
	bool									m_is_osdlp_inited=false;
	init_file_virtualization				m_f_init;
	set_fv_path								m_f_set;
};

