import pyodbc
import csv

# connect to SQL server...
#
#conn = pyodbc.connect('Data Source=Lenovo_CB;Initial Catalog=TCSWEB;Integrated Security=True')
conn = pyodbc.connect('DRIVER={ODBC Driver 17 for SQL Server};'
                      'SERVER=localhost;'
                      'DATABASE=TCSWEB;'
                      'TRUSTED_CONNECTION=yes;')

# create cursor object...
cursor = conn.cursor()

cursor.execute("DROP TABLE USERS")

# create an SQL table...
cursor.execute('''
    CREATE TABLE USERS (USERNAME VARCHAR(30) PRIMARY KEY,
    PASSWORD VARCHAR(30),
    PERMISSIONS VARCHAR(10),
    POWERUNIT VARCHAR(10),
    COMPANYKEY01 VARCHAR(10),
    COMPANYKEY02 VARCHAR(10),
    COMPANYKEY03 VARCHAR(10),
    COMPANYKEY04 VARCHAR(10),
    COMPANYKEY05 VARCHAR(10),
    MODULE01 VARCHAR(10),
    MODULE02 VARCHAR(10),
    MODULE03 VARCHAR(10),
    MODULE04 VARCHAR(10),
    MODULE05 VARCHAR(10),
    MODULE06 VARCHAR(10),
    MODULE07 VARCHAR(10),
    MODULE08 VARCHAR(10),
    MODULE09 VARCHAR(10),
    MODULE10 VARCHAR(10))
''')

users = {
    "admin": ["password", None],
    "cbraatz": ["password", 47],
    "cnormandin": ["password", 60],
    "gbraatz": ["password", 41],
    "snormandin": ["password", 58]
}

for key,value in users.items():
    query = """
        INSERT INTO dbo.USERS (
            USERNAME, PASSWORD, PERMISSIONS, POWERUNIT, 
            COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05,
            MODULE01, MODULE02, MODULE03, MODULE04, MODULE05, MODULE06, MODULE07, MODULE08, MODULE09, MODULE10
        ) 
        VALUES (
            ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?
        );
    """

    # Prepare parameters
    params = (
        key,
        value[0],
        None,  # or specify the permissions you want
        value[1] if value[1] else None,  # Use None for NULL
        'BRAUNS',  # Static value for example
        None,
        None,
        None,
        None,
        None,
        None,
        None,
        None,
        None,
        None,
        None,
        None,
        None,
        None
    )

    cursor.execute(query,params)

# commit changes...
conn.commit()

# close connection...
conn.close()