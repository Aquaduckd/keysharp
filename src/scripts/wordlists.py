import os
import requests

BASE_URL = 'https://monkeytype.com/languages'
OUT_DIR = 'monkeytype'

LANGUAGES = {
    'english':      '200',
    'english_1k':   '1k',
    'english_5k':   '5k',
    'english_10k':  '10k',
    'english_25k':  '25k',
    'english_450k': '450k',
}

if not os.path.exists(OUT_DIR):
    os.mkdir(OUT_DIR)

for lang, alias in LANGUAGES.items():
    data = requests.get(f'{BASE_URL}/{lang}.json').json()
    text = ' '.join(data['words'])

    with open(f'{OUT_DIR}/{alias}.txt', 'w') as f:
        f.write(text)