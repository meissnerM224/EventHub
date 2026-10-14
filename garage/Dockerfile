FROM dxflrs/garage:ddb90621c1e7e9d98cf0b4088802f31cac57587b AS garage
FROM alpine:3.21
COPY --from=garage /garage /usr/local/bin/garage