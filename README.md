# Crypto Finder
The application allows users to search an Ethereum block for all the transactions associated with an address and return the result.

## Running the web application

### Using dotnet CLI
1. Navigate to project folder and run command   
`dotnet publish -c Release`
2. Run the project using command  
`dotnet run`
3. Alternatively you can navigate to release folder  `\bin\Release\net5.0`  and run the command   
`dotnet cryptofinder.dll`
### Using docker CLI
Note: Please make sure dockers in installed on your system.

1. Navigate to project folder and run command   
`dotnet publish -c Release`
2. Build image from docker file using command  
`docker build -t cryptofinder-image -f Dockerfile .`
3. Create named container using command   
`docker create --name cryptofinder cryptofinder-image`
4. Run the container using command  
`docker run -dp 5000:80  --name cryptoapp cryptofinder-image`
5. Navigate to address  
`http://localhost:5000`


## Using the Application
    
### Search Ethereum Transactions
On the home screen the user can specify the Ethereum block number and an address and click search to search for transactions.

#### With valid block number and that has transactions associated with given address.

The user sees the transactions page with a table that lists all transactions associated with the given address.

#### With valid block number but no transactions associated with given address.

The user sees the transaction page with an empty table that has no transactions.

#### With invalid block number.
The user sees an error page. 


## Documentation
#### Swagger

The following endpoint displays Swagger:
1. `http://localhost:5000/swagger/index.html`


## Testing
Navigate to solution file folder and run the following command  
`dotnet test`
