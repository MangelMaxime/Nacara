import { readFile } from 'fs/promises'

export default async function() {
    const pkg = JSON.parse(await readFile(new URL('./../package.json', import.meta.url)));

    return pkg.version;
}
