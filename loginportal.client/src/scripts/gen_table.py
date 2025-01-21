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

# create an SQL table...
cursor.execute('''CREATE TABLE DMFSTDAT (MFSTKEY VARCHAR(20) PRIMARY KEY,STATUS VARCHAR(1),LASTUPDATE VARCHAR(14),MFSTNUMBER
VARCHAR(10),POWERUNIT VARCHAR (10),STOP SMALLINT,MFSTDATE VARCHAR(8),PRONUMBER VARCHAR(10),PRODATE VARCHAR(8),SHIPNAME VARCHAR(30),CONSNAME
VARCHAR(30),CONSADD1 VARCHAR(30),CONSADD2 VARCHAR(30) NULL,CONSCITY VARCHAR(20),CONSSTATE VARCHAR(2),CONSZIP
VARCHAR(5),TTLPCS SMALLINT,TTLYDS SMALLINT,TTLWGT SMALLINT,DLVDDATE VARCHAR(8),DLVDTIME VARCHAR(4),DLVDPCS
SMALLINT NULL,DLVDSIGN VARCHAR(30),DLVDNOTE VARCHAR(30),DLVDIMGFILELOCN VARCHAR(30),DLVDIMGFILESIGN VARCHAR(30))''')

# generate query string...
#
# open CSV file...
with open('dmfstdat.csv','r') as f:
    reader = csv.reader(f)

    # skip headers, maintain just in case...
    headers = next(reader)

    # skip buffer...
    next(reader)

    # initialize int columns...
    int_col = [5,16,17,18,21]
    
    # iterate each CSV row and build query string from contents...
    for row in reader:
        col_index = 0
        query = "("
        for col in row:
            # column is INT data...
            if col_index in int_col:
                query += row[col_index].strip()

            # column is NULL data...
            elif row[col_index].strip() == "NULL":
                query += row[col_index].strip()
            
            # column is empty...
            elif len(row[col_index].strip()) == 0:
                query += "NULL"

            # column is VARCHAR/Non-NULL data...
            else:
                query += "'" + row[col_index].strip() + "'"

            if col_index < len(row)-1:
                query += ","

            col_index += 1
            
        # end row query line...
        query += ");"

        # insert data into table...
        cursor.execute('INSERT INTO dbo.DMFSTDAT VALUES' + query)

# close CSV file...
f.close()

# insert data manually or as group (limit of 1000 row entries)...
#
#cursor.execute('INSERT INTO dbo.DMFSTDAT values' + query)
'''
cursor.execute(INSERT INTO dbo.DMFSTDAT values('045X021624001','0','20240201123000','045X021624',
               '045',1,'02162024','41750686','02152024','DOOLITTLE CARPET & PAINT','MOHAWK WHSE/MENDOTA HEIGHTS',
               '2359 WATERS DRIVE',NULL,'MENDOTA HEIGHTS','MN','55120',NULL,NULL,NULL,NULL,NULL,NULL,
               NULL,NULL,NULL,NULL))
'''

# commit changes...
conn.commit()

# close connection...
conn.close()