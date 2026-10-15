const dbName = "tric";
const storeName = "kv";
let dbPromise;

function open() {
    dbPromise ??= new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, 1);
        request.onupgradeneeded = () => request.result.createObjectStore(storeName);
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
    return dbPromise;
}

async function run(mode, action) {
    const db = await open();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, mode);
        const result = action(tx.objectStore(storeName));
        tx.oncomplete = () => resolve(result?.result ?? null);
        tx.onerror = () => reject(tx.error);
        tx.onabort = () => reject(tx.error);
    });
}

export const get = key => run("readonly", store => store.get(key));
export const set = (key, value) => run("readwrite", store => store.put(value, key));
export const remove = key => run("readwrite", store => store.delete(key));
export const removeMany = keys => run("readwrite", store => { keys.forEach(k => store.delete(k)); });
export const keys = () => run("readonly", store => store.getAllKeys());
export const clear = () => run("readwrite", store => store.clear());

export async function estimate() {
    const e = await navigator.storage?.estimate?.();
    return e ? [e.usage ?? 0, e.quota ?? 0] : [0, 0];
}
