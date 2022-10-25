#pragma once
#include <mutex>
/*
	Wrapper Win32 API: FindFirstChangeNotification,
					   FindNextChangeNotification,
					   FindCloseChangeNotification,
					   WaitForMultipleObjects,
					   CreateEventW,
*/
class DirMonitor {

public:
	DirMonitor();
	~DirMonitor();
public:

	bool AddDir(const std::wstring& dir, std::function<void(const std::wstring&)> func);

	void StartWork();
	inline void MarkStop();

	inline void Join() {
		try {
			if (worker.joinable()) {
				worker.join();
			}
		}
		catch(std::exception&){
			// ignored
		}
	}		

private:
	void _threadwork();

private:
	std::vector<std::tuple<std::wstring,HANDLE, std::function<void(const std::wstring&)>>> dirs;
	std::recursive_mutex res_locker;
	std::thread worker;
	HANDLE interrupt_event;
	bool bInterrupt;

};

