set -euo pipefail

API="${1:-http://localhost:5000}"
COUNT="${2:-500}"

EMAIL="bench-organizer@test.de"
PASSWORD="Test1234!"

echo "Organizer anlegen oder einloggen …"
TOKEN=$(curl -s -X POST "$API/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",
       \"displayName\":\"Bench Organizer\",\"role\":\"Organizer\"}" \
  | jq -r '.accessToken // empty')

if [[ -z "$TOKEN" ]]; then
    TOKEN=$(curl -s -X POST "$API/api/auth/login" \
      -H "Content-Type: application/json" \
      -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" \
      | jq -r '.accessToken // empty')
fi

[[ -n "$TOKEN" ]] || { echo "Kein Token erhalten – läuft die API?" >&2; exit 1; }

LOCATIONS=("Theater Kassel" "Kulturbahnhof" "Documenta Halle" "Schlosspark" "Stadthalle")

echo "$COUNT Events anlegen …"
created=0
for i in $(seq "$COUNT"); do
    days=$(( (RANDOM % 180) + 1 ))
    category=$(( (RANDOM % 3) + 1 ))
    location="${LOCATIONS[$((RANDOM % ${#LOCATIONS[@]}))]}"
    starts_at=$(date -u -d "+${days} days" +"%Y-%m-%dT19:00:00Z")
    doors_at=$(date -u -d "+${days} days" +"%Y-%m-%dT18:00:00Z")

    code=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$API/api/events" \
      -H "Content-Type: application/json" \
      -H "Authorization: Bearer $TOKEN" \
      -d "{\"title\":\"Bench Event $i\",
           \"description\":\"Generiert von seed-dev.sh\",
           \"startAt\":\"$starts_at\",
           \"doorsOpenAt\":\"$doors_at\",
           \"location\":\"$location\",
           \"maxParticipants\":$(( (RANDOM % 200) + 10 )),
           \"categoryId\":$category}")

    [[ "$code" == "201" ]] && created=$((created + 1))
    (( i % 50 == 0 )) && echo "  $i / $COUNT"
done

echo "Fertig: $created von $COUNT angelegt."
curl -s "$API/api/events" | jq 'length' | xargs -I{} echo "GET /api/events liefert {} Einträge."