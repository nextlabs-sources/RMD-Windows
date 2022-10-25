
class Inxrmaddin : public IClassFactory
{
public:
	Inxrmaddin(): m_uRefCount(1),m_uLockCount(0) {}
	~Inxrmaddin() {}

	STDMETHODIMP QueryInterface(REFIID riid, void **ppobj);

	STDMETHODIMP_(ULONG) AddRef();

	STDMETHODIMP_(ULONG) Release();

	STDMETHODIMP CreateInstance(IUnknown * pUnkOuter, REFIID riid, void ** ppvObject);

	STDMETHODIMP LockServer(BOOL fLock);

private:
	ULONG				m_uRefCount;
	ULONG				m_uLockCount;
};

