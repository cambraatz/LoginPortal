import csv

with open('dmfstdat.csv','r') as f:
    reader = csv.reader(f)

    headers = next(reader)
    next(reader)
    query = ""

    for row in reader:
        col_index = 0
        for col in row:
            #header = headers[col_index]
            query += row[col_index].strip()
            if col_index < len(row)-1:
                query += ","
            col_index += 1
        query += ";"

        '''
        MFSTKEY = row[0].strip()
        STATUS = row[1].strip()
        LASTUPDATE = row[2].strip()
        MFSTNUMBER = row[3].strip()
        POWERUNIT = row[4].strip()
        STOP = row[5].strip()
        MFSTDATE = row[6].strip()
        PRONUMBER = row[7].strip()
        PRODATE = row[8].strip()
        SHIPNAME = row[9].strip()
        CONSNAME = row[10].strip()
        CONSADD1 = row[11].strip()
        CONSADD2 = row[12].strip()
        CONSCITY = row[13].strip()
        CONSSTATE = row[14].strip()
        CONSZIP = row[15].strip()
        TTLPCS = row[16].strip()
        TTLYDS = row[17].strip()
        TTLWGT = row[18].strip()
        DLVDDATE = row[19].strip()
        DLVDTIME = row[20].strip()
        DLVDPCS = row[21].strip()
        DLVDSIGN = row[22].strip()
        DLVDNOTE = row[23].strip()
        DLVDIMGFILELOCN = row[24].strip()
        DLVDIMGFILESIGN = row[25].strip()
        '''
        
        #print(f'MFSTKEY: {MFSTKEY}, STATUS: {STATUS}, LASTUPDATE: {LASTUPDATE}, MFSTNUMBER: {MFSTNUMBER}, POWERUNIT: {POWERUNIT}')
    print(query)

f.close()