

#ifndef __NX_COMMON_RESULT_H__
#define __NX_COMMON_RESULT_H__

#include <string>

namespace NX {


class Result
{
public:
    Result() : _code(0), _line(0), _file(nullptr), _func(nullptr)
    {
    }
    explicit Result(int code, int line, const char* file, const char* func, const std::string& msg= std::string()) : _code(code), _line(line), _file(file), _func(func), _msg(msg)
    {
    }
    Result(const Result& rhs) : _code(rhs._code), _line(rhs._line), _file(rhs._file), _func(rhs._func), _msg(rhs._msg)
    {
    }
    virtual ~Result()
    {
    }

    inline int GetCode() const { return _code; }
    inline int GetLine() const { return _line; }
    inline const char* GetFile() const { return _file ? _file : ""; }
    inline const char* GetFunction() const { return _func ? _func : ""; }
    inline const std::string& GetMsg() const { return _msg; }

    inline operator bool() const { return (0 == _code); }
    inline bool operator == (int code) const { return (code == _code); }
    inline bool operator != (int code) const { return (code != _code); }

    virtual void Clear()
    {
        _code = 0;
        _line = 0;
        _file = nullptr;
        _func = nullptr;
        _msg.clear();
    }

    Result& operator = (const Result& rhs)
    {
        if (this != &rhs)
        {
            _code = rhs._code;
            _line = rhs._line;
            _file = rhs._file;
            _func = rhs._func;
            _msg  = rhs._msg;
        }
        return *this;
    }

protected:
    int _MoveCode() { int tmp = _code; _code = 0; return tmp; }
    int _MoveLine() { int tmp = _line; _line = 0; return tmp; }
    const char* _MoveFile() { const char* tmp = _file; _file = 0; return tmp; }
    const char* _MoveFunc() { const char* tmp = _func; _func = 0; return tmp; }


private:
    int _code;
    int _line;
    const char* _file;
    const char* _func;
    std::string _msg;
};

#define RESULT(c)       NX::Result(c, __LINE__, __FILE__, __FUNCTION__, "")
#define RESULT2(c, m)   NX::Result(c, __LINE__, __FILE__, __FUNCTION__, m)

}   // NX

#endif
