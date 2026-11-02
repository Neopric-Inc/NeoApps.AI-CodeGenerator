"""Demo data for SplitShare: python3 seed.py <admin-username> <admin-password> (API on localhost:5100)."""
import json, urllib.request
B="http://localhost:5100/v1/api/"
def call(m,p,body=None,tok=None):
    r=urllib.request.Request(B+p,method=m,data=json.dumps(body).encode() if body is not None else None,headers={"Content-Type":"application/json",**({"Authorization":"Bearer "+tok} if tok else {})})
    try:
        with urllib.request.urlopen(r) as x: return x.status, json.loads(x.read() or b"null")
    except urllib.error.HTTPError as e: return e.code, e.read()[:200]
import sys
tok=call("POST","Token",{"username":sys.argv[1],"password":sys.argv[2]})[1]["document"]["accessToken"]
def new(t,d):
    s,r=call("POST",t,d,tok); assert s==200 and r["code"]==1,(t,s,r); return r["document"]
# rename probe group
new("Expense_groups",{"name":"Roommates","group_type":"home","default_currency":"INR","description":"Flat 4B, Koramangala"})
goa=new("Expense_groups",{"name":"Goa trip","group_type":"trip","default_currency":"INR","description":"Oct long weekend"})
people={}
for n,e,p in [("Asha Rao","asha@example.com","+91 98450 11111"),("Ravi Kumar","ravi@example.com","+91 98450 22222"),("Meera Shah","meera@example.com","+91 98450 33333"),("Kabir Das","kabir@example.com","+91 98450 44444")]:
    people[n.split()[0]]=new("Members",{"full_name":n,"email":e,"phone":p,"login_username":{"Asha":"asha","Ravi":"ravi"}.get(n.split()[0])})
for n in ["Asha","Ravi","Meera"]: new("Group_members",{"expense_group_id":1,"member_id":people[n],"joined_on":"2026-06-01T00:00:00"})
for n in ["Asha","Ravi","Meera","Kabir"]: new("Group_members",{"expense_group_id":goa,"member_id":people[n],"joined_on":"2026-10-01T00:00:00"})
def exp(g,desc,amt,date,cat,payer,st="equal",notes=None,splits=None):
    eid=new("Expenses",{"expense_group_id":g,"description":desc,"amount":amt,"currency":"INR","expense_date":date+"T00:00:00","category":cat,"paid_by_member_id":people[payer],"split_type":st,"split_status":"split" if splits else "not_split","notes":notes})
    for m,v,owed in (splits or []):
        new("Expense_splits",{"expense_id":eid,"member_id":people[m],"share_value":v,"owed_amount":owed,"status":"settled" if m==payer else "unsettled"})
    return eid
e1=exp(1,"November rent",45000,"2026-11-01","rent","Asha","equal",None,[("Asha",None,15000),("Ravi",None,15000),("Meera",None,15000)])
exp(1,"Electricity bill",3600,"2026-10-28","utilities","Ravi","equal",None,[("Asha",None,1200),("Ravi",None,1200),("Meera",None,1200)])
exp(1,"Groceries - BigBasket",2400,"2026-11-01","food","Meera","percentage","Asha 50%, Ravi 25%, Meera 25%",[("Asha",50,1200),("Ravi",25,600),("Meera",25,600)])
exp(1,"Swiggy dinner",1350,"2026-11-02","food","Asha","equal")
exp(goa,"Villa booking",24000,"2026-10-10","travel","Kabir","equal",None,[(m,None,6000) for m in ["Asha","Ravi","Meera","Kabir"]])
exp(goa,"Seafood at Fisherman's Wharf",4800,"2026-10-11","food","Ravi","shares","Asha 1, Ravi 1, Meera 1, Kabir 1",[(m,1,1200) for m in ["Asha","Ravi","Meera","Kabir"]])
exp(goa,"Scooter rentals",3000,"2026-10-11","travel","Meera","exact","Asha 1000, Ravi 1000, Kabir 1000",[("Asha",1000,1000),("Ravi",1000,1000),("Kabir",1000,1000)])
exp(goa,"Cruise tickets",2800,"2026-10-12","entertainment","Asha","equal")
new("Settlements",{"expense_group_id":goa,"from_member_id":people["Asha"],"to_member_id":people["Kabir"],"amount":6000,"currency":"INR","settlement_date":"2026-10-15T00:00:00","method":"upi","status":"completed","notes":"Villa share"})
new("Settlements",{"expense_group_id":1,"from_member_id":people["Ravi"],"to_member_id":people["Asha"],"amount":5000,"currency":"INR","settlement_date":"2026-11-02T00:00:00","method":"bank_transfer","status":"pending"})
new("Expense_comments",{"expense_id":e1,"member_id":people["Ravi"],"comment_text":"Paying my share by the 5th."})
print("seeded", people, goa)

import secrets
# Demo logins (role 2 = Member, 3 = Viewer). Passwords are random and printed once.
for u,r in [("asha",2),("ravi",2),("guest",3)]:
    pw=secrets.token_urlsafe(9)+"@1"
    s,res=call("POST","Access/members",{"username":u,"password":pw,"role_id":r},tok)
    print(f"login {u} / {pw}" if s==200 else (u,s,res))
