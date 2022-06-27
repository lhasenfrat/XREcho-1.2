import csv
with open('gazeData.csv','r')as file:
   filecontent=list(csv.reader(file))

   newfile=open('newGazeData.csv','w')
   writer=csv.writer(newfile,lineterminator='\n')
   writer.writerow(filecontent[0])

   for i in range(1,len(filecontent)-1):
      if (filecontent[i][1]!=filecontent[i-1][1]) ^ (filecontent[i][1]!=filecontent[i+1][1]) :
         writer.writerow(filecontent[i])

   writer.writerow(filecontent[-1])
   newfile.close()

