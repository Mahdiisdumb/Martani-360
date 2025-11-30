const fs = require('fs');
const path = require('path');

// ================= CONFIG =================
// Set your Unity project root here:
const projectRoot = 'C:/Users/Mahdi/Martani 360/Martani-360';

// Set to true if you also want to delete .meta files (WARNING: breaks references!)
const deleteMetaFiles = true;

// ================= SCRIPT =================
function deleteFilesAndFolders(dir) {
    if (!fs.existsSync(dir)) return;

    const items = fs.readdirSync(dir, { withFileTypes: true });

    for (const item of items) {
        const fullPath = path.join(dir, item.name);

        try {
            if (item.isDirectory()) {
                // Skip Library/Temp/obj folders
                deleteFilesAndFolders(fullPath);

                // Remove empty folder
                if (fs.readdirSync(fullPath).length === 0) {
                    fs.rmdirSync(fullPath);
                    console.log(`Removed empty folder: ${fullPath}`);
                }
            } else if (item.isFile()) {
                const ext = path.extname(item.name).toLowerCase();

                if (
                    deleteMetaFiles && ext === '.meta' || // optional .meta files
                    ['.pidb', '.userprefs'].includes(ext) || // temp Unity files
                    ['.ds_store', '.db', '.ini', '.log'].includes(ext) // OS junk/logs
                ) {
                    fs.unlinkSync(fullPath);
                    console.log(`Deleted file: ${fullPath}`);
                }
            }
        } catch (err) {
            console.error(`Failed to delete ${fullPath}:`, err.message);
        }
    }
}

// ================= SAFE FOLDERS TO DELETE =================
const safeFolders = ['Library', 'Temp', 'obj', 'Build', 'Builds', '.vs', '.vscode', '.idea'];

for (const folder of safeFolders) {
    const fullPath = path.join(projectRoot, folder);
    if (fs.existsSync(fullPath)) {
        try {
            fs.rmSync(fullPath, { recursive: true, force: true });
            console.log(`Deleted folder: ${fullPath}`);
        } catch (err) {
            console.error(`Failed to delete folder ${fullPath}:`, err.message);
        }
    }
}

// ================= RECURSIVE CLEANUP =================
deleteFilesAndFolders(projectRoot);

console.log('Unity temp/backup cleanup complete.');
