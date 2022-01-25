openssl x509 -inform DER -in /root/.aspnet/https/root.cer -out /root/.aspnet/https/root.crt
cp /root/.aspnet/https/root.crt /usr/local/share/ca-certificates/
update-ca-certificates

dotnet api.dll