// combined-init.js
// 1) Initiate the config server replica-set
var cfg = {
    _id: "configReplSet",
    configsvr: true,
    members: [
        { _id: 0, host: "mongo-configsvr:27019" }
    ]
};
new Mongo("mongo-configsvr:27019").getDB("admin").runCommand({ replSetInitiate: cfg });

// 2) Initiate shard1 replica-set
var shard1 = {
    _id: "shard1ReplSet",
    members: [
        { _id: 0, host: "mongo-shard1:27017" }
    ]
};
new Mongo("mongo-shard1:27017").getDB("admin").runCommand({ replSetInitiate: shard1 });

// 3) Initiate shard2 replica-set
var shard2 = {
    _id: "shard2ReplSet",
    members: [
        { _id: 0, host: "mongo-shard2:27017" }
    ]
};
new Mongo("mongo-shard2:27017").getDB("admin").runCommand({ replSetInitiate: shard2 });

// 4) Add both shards to the cluster via mongos
var mongos = new Mongo("mongos:27017").getDB("admin");
mongos.runCommand({ addShard: "shard1ReplSet/mongo-shard1:27017" });
mongos.runCommand({ addShard: "shard2ReplSet/mongo-shard2:27017" });
