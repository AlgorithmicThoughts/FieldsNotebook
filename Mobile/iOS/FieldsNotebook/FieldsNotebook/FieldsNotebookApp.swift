//
//  FieldsNotebookApp.swift
//  FieldsNotebook
//
//  Created by Ravi on 10/10/26.
//

import SwiftUI
import CoreData

@main
struct FieldsNotebookApp: App {
    let persistenceController = PersistenceController.shared

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environment(\.managedObjectContext, persistenceController.container.viewContext)
        }
    }
}
