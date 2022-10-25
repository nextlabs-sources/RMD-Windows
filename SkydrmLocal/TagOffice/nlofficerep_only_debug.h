
/**
*  @file this a debug file.
*
*  all the debug tools at here: debug log, trace.
*  Log system, Warning box both a static library. 
*
*  @author NextLabs::kim
*/

#ifndef NLOFFICEREP_ONLY_DEBUG_H_
#define NLOFFICEREP_ONLY_DEBUG_H_

#include <string>
#include <vector>

/** Debug compile information */
#define chSTR2(x) #x
#define chSTR(x)  chSTR2(x)

#if 0
#define chMSG(desc) message("")
#else
#define chMSG(desc) message( __FILE__ "(" chSTR(__LINE__) "):" #desc )
#endif

/** Trace */
#define NLONLY_DEBUGEX(X) CFunTraceLog X( __FUNCTIONW__, __LINE__ );

#define NLONLY_DEBUG CFunTraceLog OnlyForDebugLog( __FUNCTIONW__, __LINE__ );

/** Debug log */
#define NLPRINT_DEBUGVIEWLOGEX( bOutputDebugLog, ... ) NLPrintLogW(bOutputDebugLog,    L"\t %s:[%s:%s:%d]>:", g_pKwchDebugLogHeader, __FUNCTIONW__, __FILEW__, __LINE__), NLPrintLogW(bOutputDebugLog,    __VA_ARGS__)
#define NLPRINT_DEBUGVIEWLOG( ... )                    NLPrintLogW(g_kbOutputDebugLog, L"\t %s:[%s:%s:%d]>:", g_pKwchDebugLogHeader, __FUNCTIONW__, __FILEW__, __LINE__), NLPrintLogW(g_kbOutputDebugLog, __VA_ARGS__)

#define NLPRINT_TAGPAIRLOG(vecTagPair, pwchBeginFlag, pwchEndFlag) NLPrintLogW(g_kbOutputDebugLog, L"\t %s:[%s:%d]>:", g_pKwchDebugLogHeader, __FUNCTIONW__, __LINE__), NLPrintTagPairW(vecTagPair, pwchBeginFlag, pwchEndFlag)


extern const wchar_t* g_pKwchDebugLogHeader;
extern const bool g_kbOutputDebugLog;
extern const bool g_kbOutputEventViewLog;

class CFunTraceLog
{
public:
    CFunTraceLog(_In_ const wchar_t* const pkwchFuncName, _In_ const unsigned int unlLine);
    ~CFunTraceLog();
private:
    std::wstring m_wstrFuncName;
    unsigned int m_unlStartLine;
};

wchar_t* NLConvertBoolToString(_In_ const bool bIsTtrue);

void NLPrintLogW(_In_ const bool bOutputDebugLog, _In_ const wchar_t* _Fmt, ...);

void NLPrintTagPairW(_In_ const std::vector<std::pair<std::wstring, std::wstring>>& kvecTagPair, _In_opt_ const wchar_t* pkwchBeginFlag = NULL, _In_opt_ const wchar_t* pkwchEndFlag = NULL);

void NLOutputDebugStringW(_In_ wchar_t* pwchDebugLog);

/** Debug message box */
int NLMessageBoxW(_In_opt_ const wchar_t* pkwchText, _In_opt_ const wchar_t* pkwchCaption = L"KIM_TEST", _In_ unsigned int uType = MB_OK|MB_TOPMOST);


#endif /* NLOFFICEREP_ONLY_DEBUG_H_ */