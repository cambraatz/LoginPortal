import pyodbc

# connect to SQL server...
#
#conn = pyodbc.connect('Data Source=Lenovo_CB;Initial Catalog=TCSWEB;Integrated Security=True')
conn = pyodbc.connect('DRIVER={ODBC Driver 17 for SQL Server};'
                      'SERVER=localhost;'
                      'DATABASE=TCSWEB;'
                      'TRUSTED_CONNECTION=yes;')

# create cursor object...
cursor = conn.cursor()

cursor.execute("DROP TABLE COMPANY")

# create an SQL table...
cursor.execute('''CREATE TABLE COMPANY (COMPANYKEY VARCHAR(10) PRIMARY KEY,COMPANYNAME VARCHAR(50),COMPANYDB VARCHAR(10))''')

users = {
    "COMPANY01": ["BRAUNS","Brauns Express Inc"],
    "COMPANY02": ["NTS", "Normandin Trucking Support"],
    "COMPANY03": ["Transportaion Computer Support, LLC", "TCSWEB"]
}

for key,value in users.items():
    query = "INSERT INTO dbo.COMPANY(COMPANYKEY,COMPANYNAME) VALUES (?,?);"

    # Prepare parameters
    params = (key, value[0], value[1])

    cursor.execute(query,params)

# commit changes...
conn.commit()

# close connection...
conn.close()