#include "stdafx.h"
#include <iostream>
#include <tuple>
#include "Monitor.h"

using namespace std;

bool is_dir_exist(const char * dir)
{
	DWORD dwAttrib = GetFileAttributesA(dir);
	return INVALID_FILE_ATTRIBUTES != dwAttrib && 0 != (dwAttrib & FILE_ATTRIBUTE_DIRECTORY);
}

bool is_dir_exist(const wchar_t * dir)
{
	DWORD dwAttrib = GetFileAttributesW(dir);
	return INVALID_FILE_ATTRIBUTES != dwAttrib && 0 != (dwAttrib & FILE_ATTRIBUTE_DIRECTORY);
}


DirMonitor::DirMonitor() :bInterrupt(false), interrupt_event(NULL){
	interrupt_event = ::CreateEventW(NULL, TRUE, FALSE, NULL);
}

DirMonitor::~DirMonitor() {
	if (worker.joinable()) {
		MarkStop();
		Join();
	}
	::CloseHandle(interrupt_event);
}

bool DirMonitor::AddDir(const std::wstring& dir, std::function<void(const std::wstring&)> func)
{
	if (!is_dir_exist(dir.c_str())) {
		return false;
	}
	std::lock_guard<std::recursive_mutex> lock(this->res_locker);
	this->dirs.push_back(std::make_tuple(dir, (HANDLE)NULL,func));
	return true;

}

void DirMonitor::StartWork()
{
	if (!worker.joinable()) {
		this->worker = std::thread(&DirMonitor::_threadwork, this);
	}
	else {
		MarkStop();
		Join();
		this->worker = std::thread(&DirMonitor::_threadwork, this);
	}
}

void DirMonitor::MarkStop() { 
	bInterrupt = true; 
	::SetEvent(this->interrupt_event);
}

//
// make sure thread fucntion and this class Res is seperated
//
void DirMonitor::_threadwork()
{
	if (this->bInterrupt) {
		return;
	}

	// copy Res from this class
	std::vector<std::tuple<std::wstring, HANDLE, std::function<void(const std::wstring&)>>> dirs;
	{
		std::lock_guard<std::recursive_mutex> lock(this->res_locker);
		dirs = this->dirs;
	}


	// "Monitor class's work thread";
	const DWORD MonitorParam = 
		FILE_NOTIFY_CHANGE_FILE_NAME |	// any files in folder, renaming, creating, or deleting 
		FILE_NOTIFY_CHANGE_DIR_NAME |   // any subfolders in folder, creating or deleting
		FILE_NOTIFY_CHANGE_SIZE |   // any file in this subtree, changing
		FILE_NOTIFY_CHANGE_LAST_WRITE;

	for (auto& p : dirs) {
		std::get<1>(p) = ::FindFirstChangeNotificationW(std::get<0>(p).c_str(), true, MonitorParam);
	}

	// prepare data
	std::vector<HANDLE> hs;
	for (auto& p : dirs) {
		hs.push_back(std::get<1>(p));
	}
	hs.push_back(interrupt_event);


	const DWORD waitSecs = 300000;// for 300s
	while (!this->bInterrupt) 
	{
		DWORD waitstatus = ::WaitForMultipleObjects((DWORD)hs.size(), hs.data(), false, waitSecs);
		if (waitstatus == WAIT_TIMEOUT) {
			cout << "time out for WaitForMultipleObjects, continue" << endl;
			continue;
		}
		else if (waitstatus == WAIT_FAILED) {
			cout << "thread worker, error occured";
			break;
		}
		// which one occured
		else if (waitstatus < hs.size() - 1) {
			auto& callback = std::get<2>(dirs[waitstatus]);
			if (callback) {
				auto& param = std::get<0>(dirs[waitstatus]);
				callback(param);
			}
			// continue to wait
			if (!FindNextChangeNotification(std::get<1>(dirs[waitstatus]))) {
				cout << "failed call FindNextChangeNotification" << endl;
			}
			continue;
		}
		// interrupt event fired
		else if (waitstatus == hs.size() - 1) {
			cout << "recevied interrupt_event win32 event" << endl;
			::ResetEvent(interrupt_event);
			break;
		}
		else {
			cout << "unexpected event fired" << endl;
		}
	}

	//
	cout << "clean reours in worker thread" << endl;

	// as win32 reuqired, release handle here
	for (auto& p : dirs) {
		::FindCloseChangeNotification(std::get<1>(p));
	}

	// recover interrupt
	this->bInterrupt = false;
		
}
