#pragma once

#ifdef __cplusplus
extern "C" {
#endif

	typedef struct _SHELL_GLOBAL_DATA {

		PVOID				Section;

		CRITICAL_SECTION	SectionLock;

		HMODULE				hModule;

		LONG				nxrmshellInstanceCount;

		LONG				ContextMenuInstanceCount;

		LONG				IconHandlerInstanceCount;
	}SHELL_GLOBAL_DATA, *PSHELL_GLOBAL_DATA;

#ifdef __cplusplus
}
#endif
