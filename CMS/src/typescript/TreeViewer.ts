declare var require: any;


import { Network, DataSet } from "../../node_modules/vis";

export class TreeViewer {
    m_dom_element: any;
    m_node_click_handler: any;
    m_edge_click_handler: any;
    m_node_doubleclick_handler: any;
    m_edge_doubleclick_handler: any;
    m_data: any;
    m_parent_prop = "parent";
    m_name_prop = "name";
    m_id_prop = "id";
    m_tree: any[];
    m_zoom_stack: number[] = [];
    m_client_tree_root: any;
    m_client_treenode_dict: any;
    m_nodes: DataSet<any>;
    m_edges: DataSet<any>;
    m_network: Network;
    m_node_font_size = 10;
    m_maxdepth = 3;
    m_minnodewidth = 40;
    m_maxnodewidth = 100;

    // VisJS options
    m_options = {
        autoResize: true,
        height: "100%",
        width: "100%",
        locale: "en",
        //configure: { enabled: true, showButton: true },
        //locales: locales,
        clickToUse: false,
        //configure: {...},    // defined in the configure module.
        //edges: {...},        // defined in the edges module.
        nodes: {
            widthConstraint: {
                minimum: 40,
                maximum: 100,
            },
            font: {
                size: 14,
                color: 'white',
            },
            shape: 'box',  // box, ellipse, circle, text, database
            color: {
                border: '#000000',
                background: '#444',
                highlight: {
                    border: '#F00',
                    background: '#F00'
                }
            },
        },
        //groups: {...},       // defined in the groups module.
        layout: {
            hierarchical: {
                enabled: true,
                direction: "UD",
                sortMethod: "directed"
            }
        },
        physics: {
            hierarchicalRepulsion: {
                centralGravity: 0,
                nodeDistance: 100,
            }
        },
        //interaction: {...},  // defined in the interaction module.
        //manipulation: {...}, // defined in the manipulation module.
        //physics: {...},      // defined in the physics module.
    };

    //------------------------------------------------------------
    //
    // Function:    constructor
    // Author:      Pete Humphrey
    // Description: constructor for TreeViewer
    //
    // param options:     an options object
    // 
    // options = { parent_prop: "parent",
    //             name_prop: "name",
    //             id_prop: "id",
    //             direction: "UD",
    //             maxdepth: 3,
    //             minnodewidth: 40,   // default is 40  
    //             maxnodewidth: 100,  // default is 100
    //             font_size: 24,
    //             nodedistance: 100,
    //             nodeclick: function(node) ...,
    //             nodecoubleclick: function(node) ...,
    //           }
    //
    //------------------------------------------------------------
    /** constructor for TreeViewer */
    constructor(treenodes: any[], options: any) {
        console.log("TreeView constructor", options);
        if (options.parent_prop) this.m_parent_prop = options.parent_prop;
        if (options.name_prop) this.m_name_prop = options.name_prop;
        if (options.id_prop) this.m_id_prop = options.id_prop;
        if (options.direction) this.m_options.layout.hierarchical.direction = options.direction;
        if (options.maxdepth) this.m_maxdepth = options.maxdepth;
        if (options.font_size) this.m_options.nodes.font.size = options.font_size;
        if (options.nodeclick) this.m_node_click_handler = options.nodeclick;
        if (options.nodedistance) this.m_options.physics.hierarchicalRepulsion.nodeDistance = options.nodedistance;
        if (options.nodedoubleclick) this.m_node_doubleclick_handler = options.nodedoubleclick;
        if (options.maxnodewidth) this.m_maxnodewidth = options.maxnodewidth;
        if (options.minnodewidth) this.m_minnodewidth = options.minnodewidth;
        this.m_tree = treenodes;
        this.m_client_tree_root = treenodes.filter(x => {
            let parent_id = x[options.parent_prop];
            return (!parent_id);
        })[0];

        console.log("TreeViewer constructor:", this.m_client_tree_root, this.m_tree);

        // create a map of client treenode id => client treenode
        // the VisJS nodes will use the same id's
        let client_treenode_dict = {};
        this.m_tree.forEach(function (x) {
            let id: number = x[options.id_prop];
            client_treenode_dict[id] = x;
        });
        this.m_client_treenode_dict = client_treenode_dict;
        this.build(this.m_client_tree_root);
    }

    //----------------------------------------------------------------------
    //
    // Function:        build
    // Author:          Pete Humphrey
    //
    // Description:     Build the Vis.JS network for the tree
    //
    // param root_treenode   the root of the tree, or undefined for whole tree
    //
    // <remarks>
    // This method will initialize the following member variables
    //     m_nodes:         the Vis.js network nodes
    //     m_edges:         the Vis.js network edges
    //
    //----------------------------------------------------------------------
    build(root_treenode: any): void {
        let node_dict = {};
        let treenode_dict = {};
        let nodes = [];
        let edges = [];
        let next_node_id = 1;
        let next_edge_id = 1;
        let parent_prop = this.m_parent_prop;
        let name_prop = this.m_name_prop;
        let id_prop = this.m_id_prop;
        let treenodes = [];

        console.log("Building subtree");
        this.subtree(root_treenode, this.m_maxdepth, treenodes);

        console.log("build treenodes", treenodes);

        for (let i = 0; i < treenodes.length; i++) {
            let treenode = treenodes[i];
            let treenode_id = treenode[id_prop];

            treenode_dict[treenode[id_prop]] = treenode;
            let node = {
                id: treenode_id,
                label: treenode[name_prop],
                widthConstraint: { minimum: this.m_minnodewidth, maximum: this.m_maxnodewidth },
            };
            treenode.visnode = node;
            node_dict[node.id] = treenode;
            nodes.push(node);
        }
        nodes.forEach(function (node) {
            let treenode = node_dict[node.id];
            let parent_id = treenode[parent_prop];
            let parent = node_dict[parent_id];
            if (parent) {
                let parent_node = parent.visnode;
                // create an edge from the parent to this node
                let edge = {
                    id: next_edge_id++,
                    from: parent_node.id,
                    to: node.id
                };
                edges.push(edge);
            }
        });
        this.m_nodes = new DataSet(nodes);
        this.m_edges = new DataSet(edges);
    }

    //----------------------------------------------------------------------
    //
    // Function:        subtree
    // Author:          Pete Humphrey
    //
    // Description:     Extract the nodes of a subtree
    //
    // param root       root of the subtree
    // param depth      the max depth of the subtree
    // param subtreenodes an array to hold the subtreen nodes
    //
    //----------------------------------------------------------------------
    subtree(root: any, depth: number, subtreenodes: any[]): void {
        if (typeof root == "number") root = this.m_client_treenode_dict[root];
        let root_id = root[this.m_id_prop];
        let parent_prop = this.m_parent_prop;
        subtreenodes.push(root);
        if (depth > 1) {
            let children = this.m_tree.filter(n => {
                let parent_id = n[parent_prop];
                return parent_id == root_id;
            });
            for (let i = 0; i < children.length; i++) {
                this.subtree(children[i], depth - 1, subtreenodes);
            }
        }
    }

    //----------------------------------------------------------------------
    //
    // Function:        render
    // Author:          Pete Humphrey
    //
    // Description:     Render the Vis.js network
    //
    // param dom_element:   optional DOM element ID - must be passed
    //                      the first time this method is called
    //
    //----------------------------------------------------------------------
    render(dom_element: any): void {
        let nodes = this.m_nodes;
        let edges = this.m_edges;
        if (dom_element) this.m_dom_element = dom_element;
        //edges.add([{ id: 1, from: 1, to: 2 }, { id: 2, from: 1, to: 3 }, { id: 3, from: 2, to: 4 }, { id: 4, from: 2, to: 5 }]);

        // create a network
        var container = document.getElementById(this.m_dom_element);
        this.m_data = {
            nodes: this.m_nodes,
            edges: this.m_edges
        };
        this.m_network = new Network(container, this.m_data, this.m_options);

        let self = this;

        this.m_network.on("click", function (params) {
            console.log("In on_click", params);
            let node_count = params.nodes.length;
            let edge_count = params.edges.length;
            let item_count = params.items.length;

            if (node_count == 1) {
                let target_id = this.getNodeAt(params.pointer.DOM); // node id
                let target = self.m_client_treenode_dict[target_id];
                if (self.m_node_click_handler) self.m_node_click_handler(target_id, target);
                return;
            }
            if (node_count == 0 && edge_count == 1) {
                let target = params.edges[0];
                console.log("Edge Clicked", target);
                return;
            }
            if (node_count == 0 && edge_count == 0 && item_count == 0) {
                console.log("Background Clicked");
                return;
            }
            console.log("items:");
            params.items.forEach(function (x) {
                console.log("    ", x);
            });
            console.log("nodes:");
            params.nodes.forEach(function (x) {
                console.log("    ", x);
            });
            console.log("edges:");
            params.edges.forEach(function (x) {
                console.log("    ", x);
            });
            console.log("click event, getNodeAt returns: " + this.getNodeAt(params.pointer.DOM));
        });

        this.m_network.on("doubleClick", function (params) {
            let target_visnode_id = this.getNodeAt(params.pointer.DOM);
            let treenode = self.m_client_treenode_dict[target_visnode_id];
            console.log("Node Doubleclicked", target_visnode_id, treenode);
            if (self.m_node_doubleclick_handler) self.m_node_doubleclick_handler(target_visnode_id, treenode);
        });
    }

    //----------------------------------------------------------------------
    //
    // Function:        render_subtree
    // Author:          Pete Humphrey
    //
    // Description:     Render a subtree
    //
    // param node_id:   the id of the user tree to render
    //
    //----------------------------------------------------------------------
    render_subtree(node_id: any): void {
        console.log("render_subtree", node_id);
        let treenode = this.m_client_treenode_dict[node_id];
        if (treenode) {
            console.log("render_subtree:", treenode);
            let subtree_nodes = [];
            this.subtree(treenode, this.m_maxdepth, subtree_nodes);
            this.build(treenode);
            this.render(undefined);
        } else console.log("render_subtree: couldn't find subtree");
    }

    //----------------------------------------------------------------------
    //
    // Function:        zoom_to_node
    // Author:          Pete Humphrey
    //
    // Description:     Render a subtree
    //
    // param node_id:   the id of the user tree to render
    //
    //----------------------------------------------------------------------
    zoom_to_node(node_id: number): void {
        this.m_zoom_stack.push(node_id);
        this.render_subtree(node_id);
    }

    unzoom(): void {
        let target_node_id = this.m_client_tree_root[this.m_id_prop];
        if (this.m_zoom_stack.length > 0) {
            this.m_zoom_stack.pop();
            if (this.m_zoom_stack.length > 0) target_node_id = this.m_zoom_stack[this.m_zoom_stack.length - 1];
            this.render_subtree(target_node_id);
        }
    }

    get_selected_node_ids(): any[] {
        return this.m_network.getSelectedNodes();
    }
}

declare global {
    interface Window {
        TreeViewer: any;
    }
}

window.TreeViewer = TreeViewer;
