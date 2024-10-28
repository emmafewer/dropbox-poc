**Project Description**
This repo was created to mimic the file system syncing behavior of Dropbox. 

**Tech used:**
* .NET Console Application that listens to a directory on your machine for a file creation
* .NET gRPC Service that handles streaming file data in chunks


**Local Set Up Instructions**
1. Create a .env file to the Server root directory and add the following vars:
POSTGRES_PASSWORD="yourPostgresPassword"
PATH="/Users/firstNameLastName/Desktop" //can be any location of your choice
2. Set up a local PostgreSQL connection
3. Run the SQL migrations in the Server/db/scripts directory
4. Build the Solution
5. Run the Server and Client individually

**Current Functionality**
* There should be a directory on your desktop (or wherever you specified in the .env) called Client1
* You can add a file to Client1
* The file will be stored locally

***Future Functionality (partially coded)***
* There will be another directory called Client2
* When a file is added to a client directory the other client directory will sync to show the sames files 
