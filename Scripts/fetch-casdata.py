import pandas as pd
import ssl

url = 'https://en.wikipedia.org/wiki/List_of_CAS_numbers_by_chemical_compound'

# the following line fixes an SSL certificate error
ssl._create_default_https_context = ssl._create_unverified_context

def dump_table(t, fp):
    for i in range(len(t)):
        row = t.iloc[i]
        fp.write('"{0}","{1}","{2}"\n'. format(row[2], row[0], row[1]))

tables = pd.read_html(url)
fp = open('casdata.csv', 'w')
fp.write("CAS#,Formula,Name\n");
for ix,t in enumerate(tables):
    if t.shape[1] == 3:
        print('Table {0}: {1}'.format(ix, t.shape))
        dump_table(t, fp)

fp.close()
