export const $ = id => document.getElementById(id);


 export async function obtener(url) {

    const r = await fetch(url);

    if (!r.ok) {
        throw new Error(
            url + ' respondió ' + r.status
        );
    }

    return r.json();
}