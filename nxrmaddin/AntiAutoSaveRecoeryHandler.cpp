/*
    # dependent on OsDLP which provide the file virtualization functions
    # require user to provide the [SRC] and [DST] path as the pair, 
        to indicate that  if file streams will be  created and flushed into [SRC] will be 
        redirected into [DST].
*/
#include "stdafx.h"
#include "CommonFunction.h"
#include "AntiAutoSaveRecoeryHandler.h"
#include "SkyDrmSDKMgr.h"


#ifdef  _DEBUG
static const std::wstring gFolder = LR"_(C:\Users\oye\AppData\Roaming\Microsoft\Word\)_";
//static const std::wstring gRedirected = LR"_(C:\Users\oye\AppData\Roaming\Microsoft\Word\)_";
static const std::wstring gRedirected = LR"_(C:\OyeOutput\detoured_path\)_";
#endif //  _DEBUG


bool AntiAutoSaveRecoeryHandler::register_folder(const std::wstring& src_folder, const std::wstring& dst)
{
    if (!m_is_osdlp_inited) {
        DEVLOG(L"not inited\n");
        return false;
    }

    // sanity check, if recorder already
    auto it = m_folders.find(src_folder);
    if (it != m_folders.end()) {
        DEVLOG(L"had exsited\n");
        return false;
    }

    HANDLE h = INVALID_HANDLE_VALUE;
    if (!m_f_init(&h)) {
        DEVLOG(L"failed to call init in engine\n");
        return false;
    }
    
    if (!m_f_set(h, src_folder.c_str(), dst.c_str())) {
        DEVLOG(L"failed to call set in engine\n");
        return false;
    }

    // setup params
    m_hfvs.push_back(h);
    m_folders[src_folder] = dst;
    return true;
}

bool AntiAutoSaveRecoeryHandler::instance_setup()
{
    DEVLOG_FUN;
    // load library  nxosdlp
#ifdef _WIN64 
    std::wstring lib = L"nxosdlp64";
#else
    std::wstring lib = L"nxosdlp32";
#endif

    m_is_osdlp_inited = false; // assume flase at first

    // nxosdlp will be loaded by nxrmcore, it is preor than this dll
    auto h = ::LoadLibraryW(lib.c_str());
    if (h == NULL) {
        DEVLOG(L"load osdlp failed");        
        return false;
    }

    m_f_init = (init_file_virtualization)::GetProcAddress(h, "init_file_virtualization");
    if (!m_f_init) {
        return false;
    }

    m_f_set = (set_fv_path)::GetProcAddress(h, "set_fv_path");

    if (!m_f_set) {
        return false;
    }
    
    // must after call security checks
    m_is_osdlp_inited = true;
    return true;
}

void AntiAutoSaveRecoeryHandler::impl_handler()
{
    using namespace ATL;
    CComPtr<IFileOperation> spFO;

    if (FAILED(spFO.CoCreateInstance(__uuidof(FileOperation)))) {
        return;
    }
    spFO->SetOperationFlags(FOF_NO_UI);

	for (const auto x : m_folders) {
        auto p = x.second;
		// clear folder contents
        CComPtr<IShellItem> spItem;
        CComPtr<IEnumShellItems> spItems;
        if (FAILED(::SHCreateItemFromParsingName(p.c_str(), NULL, __uuidof(IShellItem), (void**)&spItem))) {
            continue;
        }
        if (FAILED(spItem->BindToHandler(NULL, BHID_EnumItems, _uuidof(IEnumShellItems), (void**)&spItems))) {
            continue;
        }
        if (FAILED(spFO->DeleteItems(spItems))) {
            continue;
        }
        spFO->PerformOperations();


        // call rmp to delete
        SkyDrmSDKMgr::Instance()->DelegateDeleteFolder(p);
	}
}
