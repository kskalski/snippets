## Example use of Google Protocol Buffers with EntityFrameworkCore to generate DAO / DB schema for EF from proto definitions

Compile (from ProtoGardenEF project dir)
```
dotnet.exe build
```

Create / apply migrations to local db file
```
dotnet.exe ef database update
```

Re-generate migrations
```
rm -fr Migrations
dotnet.exe ef migrations create Initial
```

Run
```
dotnet.exe run
```
