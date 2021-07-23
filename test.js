const { spawnSync, exec } = require('child_process');
const { resolve } = require('path');
const fs = require('fs');

deleteExistingResultFolders();
testAndCollect();
installReportGenerator();
generateReport();
openReport();

function deleteExistingResultFolders() {
    var testResultFolders = [];
    findTestResultFolders('./', testResultFolders);
    for (folder of testResultFolders) {
        console.log("Deleting " + folder);
        fs.rmdirSync(folder, { recursive: true }, function () { });
    }
}

function findTestResultFolders(dir, found) {
    var dirs = fs.readdirSync(dir, { withFileTypes: true });
    for (let d of dirs) {
        if (d.isDirectory()) {
            const path = resolve(dir, d.name);
            if (d.name.endsWith('TestResults')) {
                found.push(path);
            } else {
                findTestResultFolders(path, found);
            }
        }
    }
}


function testAndCollect() {
    spawnSync('dotnet', ['test', '--logger', 'trx'], { stdio: 'inherit', stderr: 'inherit' });
}

function installReportGenerator() {
    spawnSync('dotnet', ['new', 'tool-manifest', '--force'], { stdio: 'inherit' });
    spawnSync('dotnet', ['tool', 'install', 'dotnet-reportgenerator-globaltool'], { stdio: 'inherit' });
}

function generateReport() {
    spawnSync('dotnet', ['tool', 'run', 'reportgenerator', '-reports:**/coverage.cobertura.xml', '-targetdir:TestResult'], { stdio: 'inherit' });
}

function openReport() {
    var url = __dirname + '/TestResult/index.htm';
    var start = process.platform === 'darwin' ? 'open' : process.platform === 'win32' ? 'start' : 'xdg-open';
    exec(start + ' ' + url);
}