# Scripts

## Initializing the Database

When CAT is being installed on a new server, you must perform two main tasks.
1.  Install the CAT application<br>This will require configuring you web server (IIS, Azure, Apache, ...)
2.  Create the required MySQL databases.<br>This process is covered in this section.

### CAT Databases
CAT uses two MySQL databases, "cms" and "cmsusers".  The cmsusers database holds user account information and is managed by the Microsoft .NET Core Identity framework.

### Creating the CMS Database

In the Scripts directory, run the managedb.py Python script with the "-clean" option.  This will delete and existing cms database and create a new instance initialized with 
required tables.

```
cd Scripts
python3 managedb.py -clean -institution "<name>"
```

The value that you specify with the "-institution" flag will be the top level of you location hierarchyYou will be prompted for the user name and password for MySQL.  The script will generate
output similar to the following.

```
python3 managedb.py -clean -institution "University of New Mexico"
Enter your MySQL user name: root
Enter your MySQL password:
Run: mysql -u root -p***** -e "drop database cms"
mysql: [Warning] Using a password on the command line interface can be insecure.


########################################################################
#
# Creating clean database
#
########################################################################


Run: mysql -u root -p***** -e "create database cms CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci"
mysql: [Warning] Using a password on the command line interface can be insecure.
Run: mysql -u root -p***** cms < initializecmsdb.sql
mysql: [Warning] Using a password on the command line interface can be insecure.
Run: dotnet run -create -noprompt
Opening database ...

########################################################################
#
# In OnConfiguring
#
########################################################################

Database tables created: 1.6141931 seconds
Initializing the InventoryStatusNames table
Initializing the LocationLevelNames table
Creating the GetLocationName function
Database seeded: 0.717148 seconds
Initialize Settings table


########################################################################
#
# Populating tables
#
########################################################################


Populating ChemicalsOfInterest table
Run: python3 import-chemicals-of-concern.py -i "..\Data\Compiled Chemicals of Concern for Release SAND2019-4657 O.xlsx" -u root -p root
Populating pictograms in CASDataItems table
Run: python3 import_ghs.py -u root -p root
Updating pictograms for 15120-21-5: "GHS03,GHS05,GHS08,GHS07,Dgr" + "GHS03,GHS06,GHS05,GHS08,Dgr" = "GHS03,GHS06,GHS07,GHS08,GHS05,Dgr"
Updating pictograms for 7632-04-4: "GHS03,GHS05,GHS08,GHS07,Dgr" + "GHS03,GHS06,GHS05,GHS08,Dgr" = "GHS03,GHS06,GHS07,GHS08,GHS05,Dgr"
Updating pictograms for 11138-47-9: "GHS03,GHS05,GHS08,GHS07,Dgr" + "GHS03,GHS06,GHS05,GHS08,Dgr" = "GHS03,GHS06,GHS07,GHS08,GHS05,Dgr"
Updating pictograms for 12040-72-1: "GHS03,GHS05,GHS08,GHS07,Dgr" + "GHS03,GHS06,GHS05,GHS08,Dgr" = "GHS03,GHS06,GHS07,GHS08,GHS05,Dgr"
Updating pictograms for 10332-33-9: "GHS03,GHS05,GHS08,GHS07,Dgr" + "GHS03,GHS06,GHS05,GHS08,Dgr" = "GHS03,GHS06,GHS07,GHS08,GHS05,Dgr"
Updating pictograms for 74-90-8: "GHS02,GHS06,GHS09,Dgr" + "GHS06,GHS09,Dgr" = "GHS09,GHS06,GHS02,Dgr"
Updating pictograms for 10049-04-4: "GHS04,GHS03,GHS06,GHS05,GHS09,Dgr" + "GHS06,GHS05,GHS09,Dgr" = "GHS03,GHS06,GHS04,GHS09,GHS05,Dgr"
Updating pictograms for 7440-66-6: "GHS02,GHS09,Dgr" + "GHS09,Wng" = "GHS09,GHS02,Wng,Dgr"
Updating pictograms for 7440-43-9: "GHS06,GHS08,GHS09,Dgr" + "GHS02,GHS06,GHS08,GHS09,Dgr" = "GHS06,GHS02,GHS08,GHS09,Dgr"
Updating pictograms for 106-97-8: "GHS02,GHS04,Dgr" + "GHS02,GHS04,GHS08,Dgr" = "GHS04,GHS08,GHS02,Dgr"
Updating pictograms for 75-28-5: "GHS02,GHS04,Dgr" + "GHS02,GHS04,GHS08,Dgr" = "GHS04,GHS08,GHS02,Dgr"
Updating pictograms for 214353-17-0: "GHS05,GHS07,GHS09,Dgr" + "GHS05,GHS08,GHS07,GHS09,Dgr" = "GHS07,GHS08,GHS09,GHS05,Dgr"
Updating pictograms for 74-89-5: "GHS02,GHS04,GHS05,GHS07,Dgr" + "GHS02,GHS05,GHS07,Dgr" = "GHS07,GHS02,GHS04,GHS05,Dgr"
Updating pictograms for 124-40-3: "GHS02,GHS04,GHS05,GHS07,Dgr" + "GHS02,GHS05,GHS07,Dgr" = "GHS07,GHS02,GHS04,GHS05,Dgr"
Updating pictograms for 75-50-3: "GHS02,GHS04,GHS05,GHS07,Dgr" + "GHS02,GHS05,GHS07,Dgr" = "GHS07,GHS02,GHS04,GHS05,Dgr"
Updating pictograms for 96-91-3: "GHS01,GHS07,Dgr" + "GHS07,Wng" = "GHS07,GHS01,Wng,Dgr"
Updating pictograms for 7803-49-8: "GHS01,GHS05,GHS08,GHS07,GHS09,Dgr" + "GHS05,GHS08,GHS07,GHS09,Dgr" = "GHS07,GHS08,GHS09,GHS05,GHS01,Dgr"
Updating pictograms for 199327-61-2: "-" + "GHS08,Dgr" = "-,GHS08,Dgr"
Updating pictograms for 58594-72-2: "GHS07,GHS09,Wng" + "GHS05,GHS07,GHS09,Wng" = "GHS09,GHS05,GHS07,Wng"
Updating pictograms for 83918-57-4: "GHS07,GHS09,Wng" + "GHS05,GHS07,GHS09,Wng" = "GHS09,GHS05,GHS07,Wng"
Updating pictograms for 731-27-1: "GHS06,GHS08,GHS09,Dgr" + "GHS07,GHS09,Wng" = "GHS09,GHS08,GHS06,GHS07,Wng,Dgr"
Updating pictograms for 183196-57-8: "GHS07,Wng" + "GHS08,GHS07,Dgr" = "GHS08,GHS07,Wng,Dgr"
Updating pictograms for 78-18-2: "GHS01,GHS05,GHS07,Dgr" + "GHS02,GHS05,GHS07,Dgr" = "GHS07,GHS02,GHS05,GHS01,Dgr"
Updating pictograms for 2407-94-5: "GHS01,GHS05,GHS07,Dgr" + "GHS02,GHS05,GHS07,Dgr" = "GHS07,GHS02,GHS05,GHS01,Dgr"
Updating pictograms for 2699-11-8: "GHS01,GHS05,GHS07,Dgr" + "GHS02,GHS05,GHS07,Dgr" = "GHS07,GHS02,GHS05,GHS01,Dgr"
Updating pictograms for 12262-58-7: "GHS01,GHS05,GHS07,Dgr" + "GHS02,GHS05,GHS07,Dgr" = "GHS07,GHS02,GHS05,GHS01,Dgr"
There are 3901 rows in CASDataItems with pictograms
Max chemical name length: 464
New records: 3901   Updated records: 26
Populating disposal tables
Run: python3 import_disposal.py -u root -p root
The cms database has been created and the the Institution setting initialized to "University of New Mexico"

```

## Generating Test Data

Use the _generate-testdata.py_ to generate CAS data, locations, and inventory items for testing.

```
python3 generate-testdata.py [-for <name>] [-items <n>] [-sites <n>] [-buildings <n>] [-rooms <n>] [-stores <n>] [-shelves <n>]

python3 generate-testdata.py -help
```

This script will create the specified locations using &lt;name&gt; for the institution name. With no arguments, it defaults to:

-   Instution IRAQ
-   10,000 inventory items
-   10 sites
-   4 buildings / site
-   6 rooms / building
-   3 storage areas / room
-   5 shelves / storage area

The CASDataItems table is populated from _casdata.csv_, which can be recreated with the _fetch-casdata.py_ script.

Tables are only populated if they are empty, so running the script multiple times will not insert any addional data. Drop the database and re-run the script if you want more or less data.

## Data Download

Chemicals and CAS numbers for generating random test data were obtained from https://en.wikipedia.org/wiki/List_of_CAS_numbers_by_chemical_compound using the fetch-casdata.py script.

GHS Pictograms and Hazard data was extracted from echa_ghs_hazards.xlsx by import_ghs.py.  This Excel file comes from https://echa.europa.eu/information-on-chemicals/annex-vi-to-clp and is the May 2020 version (annex_vi_clp_table_atp13_en.xlsx on the web site).

|File|CreatedBy|CreatedFrom|Notes|
|----|---------|-----------|------|
|ghs.txt|import_ghs.py|echa_ghs_hazards.xlsx|multiple codes and cas#'s per line|
|hazard_codes.csv|import_ghs.py|echa_ghs_hazards.xlsx|one code/cas# per row|
|echa_ghs_hazards.xlsx|download|annex_vi_clp_table_atp13_en.xlsx|https://echa.europa.eu/information-on-chemicals/annex-vi-to-clp|
|HazardCodes table|import_ghs.py|echa_ghs_hazards.xsx|one code/cas# per row|

<br>

```

C:> more < ghs.txt
7: ['1333-74-0']  GHS02,GHS04,Dgr  hydrogen
8: ['16853-85-3']  GHS02,GHS05,Dgr  aluminium lithium hydride
9: ['7646-69-7']  GHS02,Dgr  sodium hydride
10: ['7789-78-8']  GHS02,Dgr  calcium hydride
11: ['7439-93-2']  GHS02,GHS05,Dgr  lithium
12: ['21369-64-2']  GHS02,GHS05,Dgr  n-hexyllithium
13: ['920-36-5']  GHS02,GHS05,GHS07,GHS09,Dgr  (2-methylpropyl)lithium
14: ['7440-41-7']  GHS06,GHS08,Dgr  beryllium
16: ['1304-56-9']  GHS06,GHS08,Dgr  beryllium oxide
17: ['7637-07-2']  GHS04,GHS06,GHS05,Dgr  boron trifluoride
18: ['10294-34-5']  GHS04,GHS06,GHS05,Dgr  boron trichloride
19: ['10294-33-4']  GHS06,GHS05,Dgr  boron tribromide
22: ['121-43-7']  GHS02,GHS07,Wng  trimethyl borate
23: ['75113-37-0']  GHS05,GHS08,GHS07,GHS09,Dgr  dibutyltin hydrogen borate
24: ['10043-35-3', '11113-50-1']  GHS08,Dgr  boric acid
25: ['1303-86-2']  GHS08,Dgr  diboron trioxide
26: ['120307-06-4']  GHS07,GHS09,Wng  tetrabutylammonium butyltriphenylborate


C:> more < hazard_codes.csv
CASNumber,HazardCode
1333-74-0, H220, Flam. Gas 1
16853-85-3, H260, Water-react. 1
16853-85-3, H314, Skin Corr. 1A
7646-69-7, H260, Water-react. 1
7789-78-8, H260, Water-react. 1
7439-93-2, H260, Water-react. 1
7439-93-2, H314, Skin Corr. 1B
21369-64-2, H250, Pyr. Sol. 1
21369-64-2, H260, Water-react. 1
21369-64-2, H314, Skin Corr. 1A
920-36-5, H250, Pyr. Liq. 1
920-36-5, H260, Water-react. 1
920-36-5, H336, STOT SE 3
920-36-5, H314, Skin Corr. 1A
920-36-5, H400, Aquatic Acute 1
920-36-5, H410, Aquatic Chronic 1
7440-41-7, H350i, Carc. 1B
7440-41-7, H330, Acute Tox. 2
7440-41-7, H301, Acute Tox. 3
7440-41-7, H335, STOT SE 3
7440-41-7, H372, STOT RE 1
7440-41-7, H315, Skin Irrit. 2
7440-41-7, H319, Eye Irrit. 2
7
```

```
mysql> select * from hazardcodes limit 20;
+--------------+---------+------------+-------------------+
| HazardCodeID | GHSCode | CASNumber  | HazardClass       |
+--------------+---------+------------+-------------------+
|            1 | H220    | 1333-74-0  | Flam. Gas 1       |
|            2 | H260    | 16853-85-3 | Water-react. 1    |
|            3 | H314    | 16853-85-3 | Skin Corr. 1A     |
|            4 | H260    | 7646-69-7  | Water-react. 1    |
|            5 | H260    | 7789-78-8  | Water-react. 1    |
|            6 | H260    | 7439-93-2  | Water-react. 1    |
|            7 | H314    | 7439-93-2  | Skin Corr. 1B     |
|            8 | H250    | 21369-64-2 | Pyr. Sol. 1       |
|            9 | H260    | 21369-64-2 | Water-react. 1    |
|           10 | H314    | 21369-64-2 | Skin Corr. 1A     |
|           11 | H250    | 920-36-5   | Pyr. Liq. 1       |
|           12 | H260    | 920-36-5   | Water-react. 1    |
|           13 | H336    | 920-36-5   | STOT SE 3         |
|           14 | H314    | 920-36-5   | Skin Corr. 1A     |
|           15 | H400    | 920-36-5   | Aquatic Acute 1   |
|           16 | H410    | 920-36-5   | Aquatic Chronic 1 |
|           17 | H350i   | 7440-41-7  | Carc. 1B          |
|           18 | H330    | 7440-41-7  | Acute Tox. 2      |
|           19 | H301    | 7440-41-7  | Acute Tox. 3      |
|           20 | H335    | 7440-41-7  | STOT SE 3         |
+--------------+---------+------------+-------------------+
20 rows in set (0.03 sec)
```
Neutralization and disposal data was extracted to Christine Straut's paper SAND2020-4485_Final Neutralization Disposal_27April2020.docx using a two-step process. First I copy/pasted data from each of the tables by hand into ../Data/disposal.xlsx. Then I converted that data into two database tables, DisposalProcedures and CASDisposalProcedures. The first has one record for each distinct disposa/neutralizatin procedure. The second maps CAS numbers to DisposalProcedures records.
