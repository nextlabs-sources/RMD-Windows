import sys
import getopt
import json
import requests # using https post get ...
import hashlib # using md5


# https://rmtest.nextlabs.solutions/rms/rs/usr
def simple_test_login(argv):
    user = 'osmond.ye@nextlabs.com'
    password = '123blue!'
    clientid = '239519F3657CA0ED0B109441BF2115DE'
    output = "D:\\OyeProject\\CSharp\\rmd-windows\\SkydrmLocal\\bin\\x64_Debug\\userJson.txt"

    #
    # parse commland line
    #
    helpstr = 'rms-login.py -u <user> -p <password> -c <clientid> -o <output>'

    try:
        # "hu:p:c:o:"   <:> has params   <hu:> -h none-parma -u has param
        opts, args = getopt.getopt(argv,"hu:p:c:o:",["user=","password=","clientid=","output="])
    except getopt.GetoptError:
        print(helpstr)
        sys.exit(1)
    for opt, arg in opts:
        if opt == '-h':
            print(helpstr)
            sys.exit()
        elif opt in ("-u","--user"):
            user = arg
        elif opt in ("-p","--password"):
            password = arg
        elif opt in ("-c","--clientid"):
            clientid = arg
        elif opt in ("-o","--output"):
            output = arg
    #
    # print parma
    #
    print('user:'+user)
    print('password:'+password)
    print('clientID:'+clientid)
    print('output:'+output)
    #
    #  prepare params
    #
    h1=hashlib.md5()
    h1.update(password.encode(encoding='utf-8'))
    passwordMD5ed = h1.hexdigest()

    server_release= "https://skydrm.com/rms/rs/usr"
    server_debug="https://rmtest.nextlabs.solutions/rms/rs/usr"

    payload={
        "email": user,
        "password": passwordMD5ed,
    }

    cookies={
        "clientId": clientid,
        "platformId": "0"
    }

    #
    # call RMS API by http_post
    #
    session=requests.session()
    requests.utils.add_dict_to_cookiejar(session.cookies,cookies)

    # use verif=False to disable ssl verification
    r = session.post(server_debug,
                      data=payload,
                     verify=False
                     )

    #
    # handle result
    #

    print(r.headers)
    #print(r.text)
    #print(r.json())

    f= open(output,mode="w")
    f.truncate(0)
    f.write(r.text)


if __name__ == "__main__":
    simple_test_login(sys.argv[1:])